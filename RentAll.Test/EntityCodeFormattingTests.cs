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

    [Fact]
    public void GetLoginPasswordAlternates_SkipsWorkOrderCodes()
    {
        var alternates = EntityCodeFormatting.GetLoginPasswordAlternates("WO-00123").ToArray();

        Assert.Single(alternates);
        Assert.Equal("WO-00123", alternates[0]);
    }
}
