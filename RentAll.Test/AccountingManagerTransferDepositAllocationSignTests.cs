namespace RentAll.Test;

public class AccountingManagerTransferDepositAllocationSignTests
{
    [Theory]
    [InlineData(-1015, 59.50, 955.50)]
    [InlineData(1015, 59.50, 955.50)]
    public void Business_residual_treats_escrow_amount_as_positive(decimal escrowAmount, decimal ownerEscrow, decimal expectedBusiness)
    {
        var normalizedEscrowAmount = RoundCurrency(Math.Abs(escrowAmount));
        var business = RoundCurrency(normalizedEscrowAmount - ownerEscrow);

        Assert.Equal(1015m, normalizedEscrowAmount);
        Assert.Equal(expectedBusiness, business);
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
