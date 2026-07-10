namespace RentAll.Domain.Models;

public class OwnerReportsBundle
{
    public OwnerCashReport Cash { get; set; } = new();
    public OwnerAccrualReport Accrual { get; set; } = new();
}
