namespace RentAll.Infrastructure.Entities.Accounting;

public class EscrowPrepaidPropertyBalanceEntity
{
    public Guid JournalEntryLineId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Balance { get; set; }
    public decimal ExpectedIncome { get; set; }
    public decimal OwnerRent { get; set; }
}
