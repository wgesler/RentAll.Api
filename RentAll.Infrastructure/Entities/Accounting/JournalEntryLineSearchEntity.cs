namespace RentAll.Infrastructure.Entities.Accounting;

public class JournalEntryLineSearchEntity
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public int ChartOfAccountId { get; set; }
    public int? CostCodeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public int OfficeId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly PostingDate { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceCode { get; set; }
    public string? JournalEntryMemo { get; set; }
    public bool IsPosted { get; set; }
    public bool IsVoided { get; set; }
}
