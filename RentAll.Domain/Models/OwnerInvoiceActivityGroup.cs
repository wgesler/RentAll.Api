namespace RentAll.Domain.Models;

internal sealed class OwnerInvoiceActivityGroup
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string AccountingPeriod { get; set; } = string.Empty;
    public string OwnerRentAccountingPeriod { get; set; } = string.Empty;
    public string InvoiceSourceCode { get; set; } = string.Empty;
    public string SourceDocumentCode { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public int? SourceTypeId { get; set; }
    public string TransactionDate { get; set; } = string.Empty;
    public long SortDateValue { get; set; }
    public decimal OwnerRentValue { get; set; }
    public decimal ExpectedIncomeValue { get; set; }
    public decimal PaymentValue { get; set; }
    public decimal OwnerPaymentReceivedValue { get; set; }
    public decimal OwnerExpenseValue { get; set; }
    public string OwnerRentMemo { get; set; } = string.Empty;
    public string OwnerExpenseMemo { get; set; } = string.Empty;
    public string OwnerPaymentMemo { get; set; } = string.Empty;
    public string PaymentMemo { get; set; } = string.Empty;
    public string OwnerRentJournalEntryCode { get; set; } = string.Empty;
    public string OwnerExpenseJournalEntryCode { get; set; } = string.Empty;
    public string OwnerPaymentJournalEntryCode { get; set; } = string.Empty;
    public string PaymentJournalEntryCode { get; set; } = string.Empty;
    public Guid? OwnerRentJournalEntryLineId { get; set; }
    public Guid? OwnerExpenseJournalEntryLineId { get; set; }
    public Guid? OwnerPaymentJournalEntryLineId { get; set; }
    public Guid? PaymentJournalEntryLineId { get; set; }
    public Guid? OwnerRentSourceId { get; set; }
    public int? OwnerRentSourceTypeId { get; set; }
    public Guid? OwnerPaymentSourceId { get; set; }
    public int? OwnerPaymentSourceTypeId { get; set; }
    public Guid? PaymentSourceId { get; set; }
    public int? PaymentSourceTypeId { get; set; }
    public Guid? OwnerExpenseSourceId { get; set; }
    public int? OwnerExpenseSourceTypeId { get; set; }
}
