namespace RentAll.Domain.Models;

public class OwnerStatementJournalEntryLine
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string ChartOfAccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
