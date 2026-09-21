using RentAll.Domain;

namespace RentAll.Test;

public class EntityCodeFormattingTests
{
    [Theory]
    [InlineData("CO-000123", "CO-000123", "CO-000000123")]
    [InlineData("CO-000000123", "CO-000000123", "CO-000123")]
    [InlineData("O-000123", "O-000123", "O-000000123")]
    [InlineData("R-000001008-001", "R-000001008-001", "R-000001008-001")]
    public void GetLoginPasswordAlternates_IncludesSixAndNineDigitForms(string input, params string[] expected)
    {
        var alternates = EntityCodeFormatting.GetLoginPasswordAlternates(input).ToArray();

        Assert.Equal(expected.Length, alternates.Length);
        foreach (var value in expected)
            Assert.Contains(value, alternates);
    }

    [Theory]
    [InlineData("DP-000049", "DP-000000049")]
    [InlineData("DP-0000049", "DP-000000049")]
    [InlineData("DP-00000049", "DP-000000049")]
    [InlineData("I-000123", "I-000000123")]
    [InlineData("R-000090-005", "R-000000090-005")]
    [InlineData("dp-000049", "DP-000000049")]
    public void CodesMatch_TreatsSixThroughNineDigitPadsAsEqual(string left, string right)
    {
        Assert.True(EntityCodeFormatting.CodesMatch(left, right));
        Assert.True(EntityCodeFormatting.CodesMatch(right, left));
    }

    [Theory]
    [InlineData("DP-000049", "DP-000000050")]
    [InlineData("I-000123", "I-000124")]
    [InlineData("R-000090-005", "R-000000090-006")]
    [InlineData("DP-000049", "PY-000000049")]
    public void CodesMatch_DoesNotMatchDifferentNumbersOrPrefixes(string left, string right)
    {
        Assert.False(EntityCodeFormatting.CodesMatch(left, right));
    }

    [Theory]
    [InlineData("WO-00123", "WO-00123", "WO-000000123", "WO-000123")]
    [InlineData("WO-000000123", "WO-000000123", "WO-000123")]
    public void GetLoginPasswordAlternates_IncludesWorkOrderSixAndNineDigitForms(string input, params string[] expected)
    {
        var alternates = EntityCodeFormatting.GetLoginPasswordAlternates(input).ToArray();

        Assert.Equal(expected.Length, alternates.Length);
        foreach (var value in expected)
            Assert.Contains(value, alternates);
    }
}
