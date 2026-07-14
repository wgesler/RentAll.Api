namespace RentAll.Domain.Models;

public class JournalEntry
{
    public Guid JournalEntryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly PostingDate { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceCode { get; set; }
    public string? CheckNumber { get; set; }
    public string? Memo { get; set; }
    public bool IsPosted { get; set; }
    public bool IsVoided { get; set; }
    public List<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
