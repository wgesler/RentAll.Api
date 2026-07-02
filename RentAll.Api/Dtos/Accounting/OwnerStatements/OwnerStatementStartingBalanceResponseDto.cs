namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementStartingBalanceResponseDto
{
    public Guid JournalEntryId { get; set; }
    public int OfficeId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public bool IsPosted { get; set; }

    public OwnerStatementStartingBalanceResponseDto(OwnerStatementStartingBalanceEntry entry)
    {
        JournalEntryId = entry.JournalEntryId;
        OfficeId = entry.OfficeId;
        OwnerId = entry.OwnerId;
        PropertyId = entry.PropertyId;
        TransactionDate = entry.TransactionDate;
        Amount = entry.Amount;
        Memo = entry.Memo;
        IsPosted = entry.IsPosted;
    }
}
