namespace RentAll.Domain.Models;

public class OwnerStatementPropertyLedgerBalance
{
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal LedgerBalance { get; set; }
}

public class CloseOwnerStatementMonthResult
{
    public int PropertiesProcessed { get; set; }
    public int JournalEntriesCreated { get; set; }
    public int JournalEntriesUpdated { get; set; }
    public bool OwnerApSoftCloseQueued { get; set; }
}
