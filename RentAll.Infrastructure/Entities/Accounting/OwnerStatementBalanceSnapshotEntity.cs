namespace RentAll.Infrastructure.Entities.Accounting;

public class OwnerStatementPropertyLedgerBalanceEntity
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal LedgerBalance { get; set; }
}
