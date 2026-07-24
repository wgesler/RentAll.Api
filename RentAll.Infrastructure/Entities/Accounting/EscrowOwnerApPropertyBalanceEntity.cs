namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowOwnerApPropertyBalanceEntity
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal ApBalance { get; set; }
}
