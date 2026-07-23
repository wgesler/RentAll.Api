namespace RentAll.Domain.Models;

public class OwnerReportsBundle
{
    public OwnerCashReport Cash { get; set; } = new();
    public OwnerAccrualReport Accrual { get; set; } = new();
    public RecapReport Recap { get; set; } = new();
    public EscrowReport Escrow { get; set; } = new();
}
