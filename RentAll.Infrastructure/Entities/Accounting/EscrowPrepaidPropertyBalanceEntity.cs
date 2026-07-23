namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowPrepaidPropertyBalanceEntity
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Balance { get; set; }
}
