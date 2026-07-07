namespace RentAll.Domain.Models;

public class JournalEntryRecapLine
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public bool IsPosted { get; set; }
    public string SourceTypeCode { get; set; } = string.Empty;
    public string SourceDocumentCode { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string ChartOfAccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string RecapCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
