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
}
