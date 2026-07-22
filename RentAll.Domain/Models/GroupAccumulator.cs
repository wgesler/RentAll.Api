namespace RentAll.Domain.Models;

internal sealed class GroupAccumulator
{
    public string PropertyCode { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public string PropertyKey { get; set; } = string.Empty;
    public string RollupSourceKey { get; set; } = string.Empty;
    public string PropertyId { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string AccountingPeriod { get; set; } = string.Empty;
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public string SourceDocumentCode { get; set; } = string.Empty;
    public string JournalEntryCode { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string OwnerRentMemo { get; set; } = string.Empty;
    public string OwnerExpenseMemo { get; set; } = string.Empty;
    public string OwnerPaymentMemo { get; set; } = string.Empty;
    public string PaymentMemo { get; set; } = string.Empty;
    public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
    public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
    public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
    public string PaymentJournalEntryCode { get; set; } = string.Empty;
    public Guid? OwnerRentJournalEntryId { get; set; }
    public Guid? OwnerRentJournalEntryLineId { get; set; }
    public Guid? OwnerExpenseJournalEntryLineId { get; set; }
    public Guid? OwnerPaymentJournalEntryLineId { get; set; }
    public Guid? PaymentJournalEntryLineId { get; set; }
    public string PaymentTransactionDate { get; set; } = string.Empty;
    public long PaymentSortDateValue { get; set; }
    public int SourcePriority { get; set; } = -1;
    public int JournalEntryPriority { get; set; } = -1;
    public int AccountingPeriodPriority { get; set; } = -1;
    public bool HasInDateRangeLine { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public long SortDateValue { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public int PostingStatusId { get; set; }
    public decimal ExpectedIncomeValue { get; set; }
    public decimal RentPlus4000Value { get; set; }
    public decimal SecurityDepositValue { get; set; }
    public decimal SdwValue { get; set; }
    public decimal FeeValue { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal PrePaymentValue { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal OwnerRentActualValue { get; set; }
    public decimal OwnerExpenseValue { get; set; }
    public decimal OwnerPaymentReceivedValue { get; set; }
    public decimal OwnerPaymentValue { get; set; }
    /// <summary>
    /// Owner portion of OwnRent for a future period that is held in PrePay received in this period (UnRec).
    /// </summary>
    public decimal PrePayOwnerUnpaidValue { get; set; }
}
