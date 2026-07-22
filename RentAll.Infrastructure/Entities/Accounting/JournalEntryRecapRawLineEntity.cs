namespace RentAll.Infrastructure.Entities.Accounting;

public class JournalEntryRecapRawLineEntity
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
    public int JournalEntryKindId { get; set; }
    public int PostingStatusId { get; set; }
    public bool IsCashOnly { get; set; }
    public string SourceTypeCode { get; set; } = string.Empty;
    public string SourceDocumentCode { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string ChartOfAccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LineMemo { get; set; }
    public string? JournalEntryMemo { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int? DefaultActRcvableAccountId { get; set; }
    public int? DefaultUndepFundsAccountId { get; set; }
    public int? DefaultPrePayAccountId { get; set; }
    public int? DefaultOwnActPayableAccountId { get; set; }
    public int? DefaultOwnerExpAccountId { get; set; }
    public int? DefaultTenantIncAccountId { get; set; }
    public bool IsRentalIncomeAccount { get; set; }
    public bool IsInDateRange { get; set; } = true;
}
