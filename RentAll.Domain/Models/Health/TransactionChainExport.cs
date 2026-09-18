namespace RentAll.Domain.Models;

public class TransactionChainExport
{
    public List<TransactionChainRow> Chain { get; set; } = [];
    public List<TransactionChainInvoice> Invoices { get; set; } = [];
    public List<TransactionChainInvoiceLine> InvoiceLines { get; set; } = [];
    public List<TransactionChainPayment> Payments { get; set; } = [];
    public List<TransactionChainDeposit> Deposits { get; set; } = [];
    public List<TransactionChainTransfer> Transfers { get; set; } = [];
    public TransactionChainSummary? Summary { get; set; }
}

public class TransactionChainRow
{
    public string OfficeName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public DateOnly? InvoiceAccountingPeriod { get; set; }
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal InvoicePaid { get; set; }
    public decimal InvoiceBalance { get; set; }
    public string? InvoiceStatus { get; set; }
    public bool InvoiceIsActive { get; set; }
    public int LedgerLineNumber { get; set; }
    public DateOnly? LedgerLineDate { get; set; }
    public string? LedgerLineDescription { get; set; }
    public decimal LedgerLineAmount { get; set; }
    public string LedgerLineType { get; set; } = string.Empty;
    public string? LedgerCostCode { get; set; }
    public string? PaymentCode { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentDescription { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentCostCode { get; set; }
    public string? PaymentStatus { get; set; }
    public bool? PaymentIsActive { get; set; }
    public string? DepositCode { get; set; }
    public DateOnly? DepositDate { get; set; }
    public DateOnly? DepositAccountingPeriod { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? DepositDescription { get; set; }
    public string? DepositBankAccount { get; set; }
    public string? DepositStatus { get; set; }
    public bool? DepositIsActive { get; set; }
    public string? TransferCode { get; set; }
    public DateOnly? TransferDate { get; set; }
    public DateOnly? TransferAccountingPeriod { get; set; }
    public decimal? TransferAmount { get; set; }
    public string? TransferDescription { get; set; }
    public string? TransferBankAccount { get; set; }
    public string? TransferStatus { get; set; }
    public bool? TransferCompleted { get; set; }
    public bool? TransferIsActive { get; set; }
}

public class TransactionChainInvoice
{
    public string OfficeName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public string? InvoicePeriod { get; set; }
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string? InvoiceStatus { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class TransactionChainInvoiceLine
{
    public string OfficeName { get; set; } = string.Empty;
    public string InvoiceCode { get; set; } = string.Empty;
    public string? ReservationCode { get; set; }
    public string? PropertyCode { get; set; }
    public string? ContactName { get; set; }
    public int LineNumber { get; set; }
    public DateOnly? LedgerLineDate { get; set; }
    public string? LineDescription { get; set; }
    public decimal LineAmount { get; set; }
    public string? CostCode { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string? PaymentCode { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal? PaymentAmount { get; set; }
}

public class TransactionChainPayment
{
    public string OfficeName { get; set; } = string.Empty;
    public string PaymentCode { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentDescription { get; set; }
    public string? PaymentType { get; set; }
    public string? CostCode { get; set; }
    public string? PaymentStatus { get; set; }
    public bool IsActive { get; set; }
    public string? DepositCode { get; set; }
    public DateOnly? DepositDate { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? TransferCode { get; set; }
    public string? InvoiceCodes { get; set; }
    public int InvoiceCount { get; set; }
}

public class TransactionChainDeposit
{
    public string OfficeName { get; set; } = string.Empty;
    public string DepositCode { get; set; } = string.Empty;
    public DateOnly DepositDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string? DepositDescription { get; set; }
    public string? BankAccount { get; set; }
    public string? DepositStatus { get; set; }
    public bool IsActive { get; set; }
    public string? TransferCode { get; set; }
    public DateOnly? TransferDate { get; set; }
    public decimal? TransferAmount { get; set; }
    public string? PaymentCodes { get; set; }
    public int PaymentCount { get; set; }
    public decimal? PaymentTotal { get; set; }
}

public class TransactionChainTransfer
{
    public string OfficeName { get; set; } = string.Empty;
    public string TransferCode { get; set; } = string.Empty;
    public DateOnly TransferDate { get; set; }
    public DateOnly AccountingPeriod { get; set; }
    public decimal Amount { get; set; }
    public string? TransferDescription { get; set; }
    public string? BankAccount { get; set; }
    public string? TransferStatus { get; set; }
    public bool TransferCompleted { get; set; }
    public bool IsActive { get; set; }
    public string? DepositCodes { get; set; }
    public int DepositCount { get; set; }
    public decimal? DepositTotal { get; set; }
    public int SplitCount { get; set; }
}

public class TransactionChainSummary
{
    public string OfficeNames { get; set; } = string.Empty;
    public DateOnly? StartDateFilter { get; set; }
    public DateOnly? EndDateFilter { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }
    public int DepositCount { get; set; }
    public int TransferCount { get; set; }
}
