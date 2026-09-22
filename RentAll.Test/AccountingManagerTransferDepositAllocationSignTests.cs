namespace RentAll.Test;

public class AccountingManagerTransferDepositAllocationSignTests
{
    [Theory]
    [InlineData(-1015, -955.50, -59.50)]
    [InlineData(1015, 955.50, 59.50)]
    public void Business_residual_preserves_escrow_sign(decimal escrowAmount, decimal ownerEscrow, decimal expectedBusiness)
    {
        var roundedEscrowAmount = RoundCurrency(escrowAmount);
        var business = RoundCurrency(roundedEscrowAmount - ownerEscrow);

        Assert.Equal(escrowAmount, roundedEscrowAmount);
        Assert.Equal(expectedBusiness, business);
    }

    [Theory]
    [InlineData(-1100, 1100, -1100)]
    [InlineData(85, 85, 85)]
    public void Classification_aligns_with_refund_or_payment_split_sign(
        decimal splitAmount,
        decimal classifiedOwnerEscrow,
        decimal expectedOwnerEscrow)
    {
        var ownerEscrow = classifiedOwnerEscrow;
        AlignTransferDepositClassificationWithSplitSign(splitAmount, ref ownerEscrow);

        Assert.Equal(expectedOwnerEscrow, ownerEscrow);
    }

    private static void AlignTransferDepositClassificationWithSplitSign(decimal splitAmount, ref decimal ownerEscrow)
    {
        if (Math.Abs(splitAmount) <= 0.005m)
            return;

        var splitSign = Math.Sign(splitAmount);
        if (Math.Abs(ownerEscrow) > 0.005m && Math.Sign(ownerEscrow) != splitSign)
            ownerEscrow = RoundCurrency(-ownerEscrow);
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
