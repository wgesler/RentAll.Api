using RentAll.Infrastructure.Services;

namespace RentAll.Test;

public class CreditCardStatementLineParserTests
{
    [Theory]
    [InlineData("883", "883")]
    [InlineData("859", "859")]
    [InlineData("-12008", "2008")]
    [InlineData("12008", "2008")]
    [InlineData("**** **** **** 1234", "1234")]
    [InlineData("x1234", "1234")]
    public void ExtractCardLastFour_ParsesCommonUploadFormats(string input, string expected)
    {
        Assert.Equal(expected, CreditCardStatementLineParser.ExtractCardLastFour(input));
    }

    [Fact]
    public void ParseTables_UsesAccountNumberColumnNotCardMember()
    {
        var table = new List<string>
        {
            "Date\tReceipt\tDescription\tCard Member\tAccount #\tAmount\tExtended Details",
            "07/28/2026\t\tTHE HOME DEPOT LOVELAND CO\tANGELA J HEALY\t-12008\t349.00\t"
        };

        var extraction = CreditCardStatementLineParser.ParseTables([table], string.Empty);
        var line = Assert.Single(extraction.Lines);

        Assert.Equal("2008", line.CardLastFour);
    }

    [Fact]
    public void ParseTables_UsesThreeDigitCardSuffix()
    {
        var table = new List<string>
        {
            "Card\tTransaction Date\tPost Date\tDescription\tCategory\tType\tAmount\tMemo",
            "883\t7/20/2026\t7/22/2026\tVIOC GT0196\tAutomotive\tSale\t-148.62\t"
        };

        var extraction = CreditCardStatementLineParser.ParseTables([table], string.Empty);
        var line = Assert.Single(extraction.Lines);

        Assert.Equal("883", line.CardLastFour);
    }

    [Fact]
    public void ParseTables_SaleIsPositiveAndReturnIsNegative()
    {
        var table = new List<string>
        {
            "Card\tTransaction Date\tPost Date\tDescription\tCategory\tType\tAmount\tMemo",
            "0859\t8/23/2026\t8/24/2026\tCOMCAST / XFINITY\tBills & Utili\tSale\t-90.27\t",
            "0883\t8/27/2026\t8/30/2026\tTHE HOME DEPOT #1512\tRepair & M\tReturn\t50.65\t",
            "0842\t9/4/2026\t9/4/2026\tPayment Thank You - Web\t\tPayment\t2820.43\t"
        };

        var extraction = CreditCardStatementLineParser.ParseTables([table], string.Empty);

        Assert.Equal(2, extraction.Lines.Count);
        Assert.Equal(90.27m, extraction.Lines[0].Amount);
        Assert.Equal(-50.65m, extraction.Lines[1].Amount);
    }

    [Fact]
    public void ParseTables_MonthDayWithoutYearUsesCurrentYear()
    {
        var text = """
            08/23 COMCAST / XFINITY 800-266-2278 CO 90.27
            08/19 THE HOME DEPOT #1512 FORT COLLINS CO -24.91
            09/04 Payment Thank You - Web -2,820.43
            """;

        var extraction = CreditCardStatementLineParser.ParseTables([], text);
        var charge = Assert.Single(extraction.Lines, line => line.VendorName != null && line.VendorName.Contains("Comcast", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(new DateOnly(DateTime.Today.Year, 8, 23), charge.ChargeDate);
        Assert.Equal(90.27m, charge.Amount);
        Assert.Contains(extraction.Lines, line => line.Amount == -24.91m);
    }

    [Fact]
    public void ParseTables_AssignsCardFromSectionFooter()
    {
        var text = """
            09/04  Payment Thank You - Web  -2,820.43
            ANGELA HEALY
            TRANSACTIONS THIS CYCLE (CARD 0842) $2820.43- INCLUDING PAYMENTS RECEIVED
            08/23  COMCAST / XFINITY 800-266-2278 CO  90.27
            08/24  COMCAST / XFINITY 800-266-2278 CO  306.46
            RACHAEL ATENCIO
            TRANSACTIONS THIS CYCLE (CARD 0859) $396.73
            08/19  THE HOME DEPOT #1512 FORT COLLINS CO  -24.91
            08/31  MAVERIK #00642 FORT CO FORT COLLINS CO  120.71
            CALVIN VANDERHOFF
            TRANSACTIONS THIS CYCLE (CARD 0883) $425.82
            """;

        var extraction = CreditCardStatementLineParser.ParseTables([], text);
        var comcast = extraction.Lines.Where(line => line.VendorName != null && line.VendorName.Contains("Comcast", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(2, comcast.Count);
        Assert.All(comcast, line => Assert.Equal("0859", line.CardLastFour));
        Assert.Equal("0883", Assert.Single(extraction.Lines, line => line.Amount == -24.91m).CardLastFour);
        Assert.Equal("0883", Assert.Single(extraction.Lines, line => line.VendorName != null && line.VendorName.Contains("Maverik", StringComparison.OrdinalIgnoreCase)).CardLastFour);
        Assert.DoesNotContain(extraction.Lines, line => line.VendorName != null && line.VendorName.Contains("Payment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReadLines_GroupsDateVendorAmountAndSectionCard()
    {
        var stream = """
            BT
            /F1 9 Tf
            72 700 Td
            (08/24 COMCAST / XFINITY 800-266-2278 CO 306.46) Tj
            0 -14 Td
            (RACHAEL ATENCIO) Tj
            0 -14 Td
            (TRANSACTIONS THIS CYCLE \(CARD 0859\) 396.73) Tj
            ET
            """;
        var body = System.Text.Encoding.ASCII.GetBytes(stream);
        var header = System.Text.Encoding.ASCII.GetBytes($"%PDF-1.4\n<< /Length {body.Length} >>\nstream\n");
        var footer = System.Text.Encoding.ASCII.GetBytes("\nendstream\n");
        var pdf = header.Concat(body).Concat(footer).ToArray();

        var text = CreditCardStatementPdfReader.ReadLines(pdf);
        var extraction = CreditCardStatementLineParser.ParseTables([], text);
        var charge = Assert.Single(extraction.Lines);

        Assert.Equal(306.46m, charge.Amount);
        Assert.Equal("0859", charge.CardLastFour);
    }

    [Fact]
    public void ReadLines_ReadsCompressedChargePackedAgainstTextOperator()
    {
        var commands = "BT /F1 9 Tf 72 700 Td (08/24 COMCAST / XFINITY 800-266-2278 CO 306.46)Tj 0 -14 Td (TRANSACTIONS THIS CYCLE \\(CARD 0859\\) 396.73)Tj ET";
        var compressed = Compress(System.Text.Encoding.ASCII.GetBytes(commands));
        var header = System.Text.Encoding.ASCII.GetBytes($"%PDF-1.4\n<</Length {compressed.Length}/Filter/FlateDecode>>stream\n");
        var footer = System.Text.Encoding.ASCII.GetBytes("\nendstream\n");
        var pdf = header.Concat(compressed).Concat(footer).ToArray();

        var extraction = CreditCardStatementLineParser.ParseTables([], CreditCardStatementPdfReader.ReadLines(pdf));
        var charge = Assert.Single(extraction.Lines);

        Assert.Equal(306.46m, charge.Amount);
        Assert.Equal("0859", charge.CardLastFour);
    }

    private static byte[] Compress(byte[] content)
    {
        using var output = new System.IO.MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionLevel.Fastest, true))
            zlib.Write(content, 0, content.Length);
        return output.ToArray();
    }
}
