namespace RentAll.Domain.Models;

public class RecapReportRow
{
    public string PropertyCode { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public string AccountingPeriod { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string JournalEntryCode { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string OwnerRentMemo { get; set; } = string.Empty;
    public string OwnerExpenseMemo { get; set; } = string.Empty;
    public string OwnerPaymentMemo { get; set; } = string.Empty;
    public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
    public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
    public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
    public Guid? OwnerRentJournalEntryId { get; set; }
    public Guid? OwnerRentJournalEntryLineId { get; set; }
    public Guid? OwnerExpenseJournalEntryLineId { get; set; }
    public Guid? OwnerPaymentJournalEntryLineId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public bool SourceLinkable { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public string ExpectedIncome { get; set; } = string.Empty;
    public string RentPlus4000 { get; set; } = string.Empty;
    public string SecurityDeposit { get; set; } = string.Empty;
    public string Sdw { get; set; } = string.Empty;
    public string Fee { get; set; } = string.Empty;
    public string Payment { get; set; } = string.Empty;
    public string PrePayment { get; set; } = string.Empty;
    public string OwnerRent { get; set; } = string.Empty;
    public string OwnerExpense { get; set; } = string.Empty;
    public string OwnerPayment { get; set; } = string.Empty;
    public decimal ExpectedIncomeValue { get; set; }
    public decimal RentPlus4000Value { get; set; }
    public decimal SecurityDepositValue { get; set; }
    public decimal SdwValue { get; set; }
    public decimal FeeValue { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal PrePaymentValue { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal OwnerExpenseValue { get; set; }
    public decimal OwnerPaymentReceivedValue { get; set; }
    public decimal OwnerPaymentValue { get; set; }
    public string PaymentMemo { get; set; } = string.Empty;
    public string PaymentJournalEntryCode { get; set; } = string.Empty;
    public Guid? PaymentJournalEntryLineId { get; set; }
    public string PaymentTransactionDate { get; set; } = string.Empty;
    public long PaymentSortDateValue { get; set; }
    public long SortDateValue { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid? JournalEntryLineId { get; set; }
    public bool IsPosted { get; set; }
}
