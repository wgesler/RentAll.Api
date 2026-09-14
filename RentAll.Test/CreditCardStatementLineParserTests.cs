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
}
