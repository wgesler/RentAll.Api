namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowNotCollectedPropertyBalanceEntity
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public decimal ExpectedIncome { get; set; }
    public decimal ActualIncome { get; set; }
    public decimal NotCollectedAmount { get; set; }
}
