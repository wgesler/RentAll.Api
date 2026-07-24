namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowNotCollectedPropertyBalanceEntity
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal NotCollectedAmount { get; set; }
}
