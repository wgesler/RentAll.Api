namespace RentAll.Domain.Models;

public class OwnerStatementStartingBalanceEntry
{
    public Guid JournalEntryId { get; set; }
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public bool IsPosted { get; set; }
}
