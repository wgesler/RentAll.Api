namespace RentAll.Infrastructure.Entities.Health;

public class TransactionChainRowEntity
{
    public string? OfficeName { get; set; }
    public string? InvoiceCode { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? InvoiceAccountingPeriod { get; set; }
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public decimal? InvoicePaid { get; set; }
    public decimal? InvoiceBalance { get; set; }
    public string? InvoiceStatus { get; set; }
    public bool? InvoiceIsActive { get; set; }
    public int? LedgerLineNumber { get; set; }
    public DateTime? LedgerLineDate { get; set; }
    public string? LedgerLineDescription { get; set; }
    public decimal? LedgerLineAmount { get; set; }
    public string? LedgerLineType { get; set; }
    public string? LedgerCostCode { get; set; }
    public string? PaymentCode { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentDescription { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentCostCode { get; set; }
    public string? PaymentStatus { get; set; }
    public bool? PaymentIsActive { get; set; }
    public string? DepositCode { get; set; }
    public DateTime? DepositDate { get; set; }
    public DateTime? DepositAccountingPeriod { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? DepositDescription { get; set; }
    public string? DepositBankAccount { get; set; }
    public string? DepositStatus { get; set; }
    public bool? DepositIsActive { get; set; }
    public string? TransferCode { get; set; }
    public DateTime? TransferDate { get; set; }
    public DateTime? TransferAccountingPeriod { get; set; }
    public decimal? TransferAmount { get; set; }
    public string? TransferDescription { get; set; }
    public string? TransferBankAccount { get; set; }
    public string? TransferStatus { get; set; }
    public bool? TransferCompleted { get; set; }
    public bool? TransferIsActive { get; set; }
}

public class TransactionChainInvoiceEntity
{
    public string? OfficeName { get; set; }
    public string? InvoiceCode { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? AccountingPeriod { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public decimal? BalanceDue { get; set; }
    public string? InvoiceStatus { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class TransactionChainInvoiceLineEntity
{
    public string? OfficeName { get; set; }
    public string? InvoiceCode { get; set; }
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public int? LineNumber { get; set; }
    public DateTime? LedgerLineDate { get; set; }
    public string? LineDescription { get; set; }
    public decimal? LineAmount { get; set; }
    public string? CostCode { get; set; }
    public string? LineType { get; set; }
    public string? PaymentCode { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? PaymentAmount { get; set; }
}

public class TransactionChainPaymentEntity
{
    public string? OfficeName { get; set; }
    public string? PaymentCode { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentDescription { get; set; }
    public string? PaymentType { get; set; }
    public string? CostCode { get; set; }
    public string? PaymentStatus { get; set; }
    public bool? IsActive { get; set; }
    public string? DepositCode { get; set; }
    public DateTime? DepositDate { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? TransferCode { get; set; }
    public string? InvoiceCodes { get; set; }
    public int? InvoiceCount { get; set; }
}

public class TransactionChainDepositEntity
{
    public string? OfficeName { get; set; }
    public string? DepositCode { get; set; }
    public DateTime? DepositDate { get; set; }
    public DateTime? AccountingPeriod { get; set; }
    public decimal? Amount { get; set; }
    public string? DepositDescription { get; set; }
    public string? BankAccount { get; set; }
    public string? DepositStatus { get; set; }
    public bool? IsActive { get; set; }
    public string? TransferCode { get; set; }
    public DateTime? TransferDate { get; set; }
    public decimal? TransferAmount { get; set; }
    public string? PaymentCodes { get; set; }
    public int? PaymentCount { get; set; }
    public decimal? PaymentTotal { get; set; }
}

public class TransactionChainTransferEntity
{
    public string? OfficeName { get; set; }
    public string? TransferCode { get; set; }
    public DateTime? TransferDate { get; set; }
    public DateTime? AccountingPeriod { get; set; }
    public decimal? Amount { get; set; }
    public string? TransferDescription { get; set; }
    public string? BankAccount { get; set; }
    public string? TransferStatus { get; set; }
    public bool? TransferCompleted { get; set; }
    public bool? IsActive { get; set; }
    public string? DepositCodes { get; set; }
    public int? DepositCount { get; set; }
    public decimal? DepositTotal { get; set; }
    public int? SplitCount { get; set; }
}

public class TransactionChainSummaryEntity
{
    public string? OfficeNames { get; set; }
    public DateTime? StartDateFilter { get; set; }
    public DateTime? EndDateFilter { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }
    public int DepositCount { get; set; }
    public int TransferCount { get; set; }
}
