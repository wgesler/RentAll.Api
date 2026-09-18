using RentAll.Infrastructure.Services;

namespace RentAll.Test;

public class ReceiptAmountParserTests
{
    [Fact]
    public void ReadLabeledTotal_PrefersGrandTotalOverSubtotal()
    {
        var content = """
            Subtotal          $40.00
            Sales Tax          $3.20
            Grand Total       $43.20
            """;

        Assert.Equal(43.20m, ReceiptAmountParser.ReadLabeledTotal(content));
    }

    [Fact]
    public void ReadLabeledTotal_UsesTotalWhenGrandTotalIsMissing()
    {
        var content = """
            Subtotal          40.00
            Tax                3.20
            Total             43.20
            """;

        Assert.Equal(43.20m, ReceiptAmountParser.ReadLabeledTotal(content));
    }

    [Fact]
    public void ReadLabeledTotal_ReadsAmountOnFollowingLine()
    {
        var content = """
            GRAND TOTAL
            $1,234.56
            """;

        Assert.Equal(1234.56m, ReceiptAmountParser.ReadLabeledTotal(content));
    }

    [Fact]
    public void ResolveAmount_DoesNotUseSubtotalWhenTotalEqualsSubtotalAndTaxExists()
    {
        var amount = ReceiptAmountParser.ResolveAmount(40.00m, 40.00m, 3.20m, "Subtotal 40.00\nTax 3.20");

        Assert.Equal(43.20m, amount);
    }

    [Fact]
    public void ResolveAmount_PrefersLabeledGrandTotalOverAzureTotal()
    {
        var amount = ReceiptAmountParser.ResolveAmount(40.00m, 40.00m, 3.20m, "Subtotal $40.00\nTax $3.20\nGrand Total $43.20");

        Assert.Equal(43.20m, amount);
    }
}
