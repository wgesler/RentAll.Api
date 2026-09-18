namespace RentAll.Api.Dtos.Health;

public class TransactionChainExportRequestDto : HealthCheckRequestDto
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public new (bool IsValid, string? ErrorMessage) IsValid()
    {
        var (baseValid, baseError) = base.IsValid();
        if (!baseValid)
            return (baseValid, baseError);

        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
            return (false, "StartDate must be on or before EndDate");

        return (true, null);
    }
}

public class TransactionChainExportDto
{
    public List<TransactionChainRowDto> Chain { get; set; } = [];
    public List<TransactionChainInvoiceDto> Invoices { get; set; } = [];
    public List<TransactionChainInvoiceLineDto> InvoiceLines { get; set; } = [];
    public List<TransactionChainPaymentDto> Payments { get; set; } = [];
    public List<TransactionChainDepositDto> Deposits { get; set; } = [];
    public List<TransactionChainTransferDto> Transfers { get; set; } = [];
    public TransactionChainSummaryDto? Summary { get; set; }

    public TransactionChainExportDto()
    {
    }

    public TransactionChainExportDto(TransactionChainExport export)
    {
        Chain = export.Chain.Select(row => new TransactionChainRowDto(row)).ToList();
        Invoices = export.Invoices.Select(row => new TransactionChainInvoiceDto(row)).ToList();
        InvoiceLines = export.InvoiceLines.Select(row => new TransactionChainInvoiceLineDto(row)).ToList();
        Payments = export.Payments.Select(row => new TransactionChainPaymentDto(row)).ToList();
        Deposits = export.Deposits.Select(row => new TransactionChainDepositDto(row)).ToList();
        Transfers = export.Transfers.Select(row => new TransactionChainTransferDto(row)).ToList();
        Summary = export.Summary == null ? null : new TransactionChainSummaryDto(export.Summary);
    }
}

public class TransactionChainRowDto
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

    public TransactionChainRowDto()
    {
    }

    public TransactionChainRowDto(TransactionChainRow row)
    {
        OfficeName = row.OfficeName;
        InvoiceCode = row.InvoiceCode;
        InvoiceDate = row.InvoiceDate;
        InvoiceAccountingPeriod = row.InvoiceAccountingPeriod;
        ReservationCode = row.ReservationCode;
        PropertyCode = row.PropertyCode;
        ContactName = row.ContactName;
        InvoiceTotal = row.InvoiceTotal;
        InvoicePaid = row.InvoicePaid;
        InvoiceBalance = row.InvoiceBalance;
        InvoiceStatus = row.InvoiceStatus;
        InvoiceIsActive = row.InvoiceIsActive;
        LedgerLineNumber = row.LedgerLineNumber;
        LedgerLineDate = row.LedgerLineDate;
        LedgerLineDescription = row.LedgerLineDescription;
        LedgerLineAmount = row.LedgerLineAmount;
        LedgerLineType = row.LedgerLineType;
        LedgerCostCode = row.LedgerCostCode;
        PaymentCode = row.PaymentCode;
        PaymentDate = row.PaymentDate;
        PaymentAmount = row.PaymentAmount;
        PaymentDescription = row.PaymentDescription;
        PaymentType = row.PaymentType;
        PaymentCostCode = row.PaymentCostCode;
        PaymentStatus = row.PaymentStatus;
        PaymentIsActive = row.PaymentIsActive;
        DepositCode = row.DepositCode;
        DepositDate = row.DepositDate;
        DepositAccountingPeriod = row.DepositAccountingPeriod;
        DepositAmount = row.DepositAmount;
        DepositDescription = row.DepositDescription;
        DepositBankAccount = row.DepositBankAccount;
        DepositStatus = row.DepositStatus;
        DepositIsActive = row.DepositIsActive;
        TransferCode = row.TransferCode;
        TransferDate = row.TransferDate;
        TransferAccountingPeriod = row.TransferAccountingPeriod;
        TransferAmount = row.TransferAmount;
        TransferDescription = row.TransferDescription;
        TransferBankAccount = row.TransferBankAccount;
        TransferStatus = row.TransferStatus;
        TransferCompleted = row.TransferCompleted;
        TransferIsActive = row.TransferIsActive;
    }
}

public class TransactionChainInvoiceDto
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

    public TransactionChainInvoiceDto()
    {
    }

    public TransactionChainInvoiceDto(TransactionChainInvoice row)
    {
        OfficeName = row.OfficeName;
        InvoiceCode = row.InvoiceCode;
        InvoiceDate = row.InvoiceDate;
        DueDate = row.DueDate;
        AccountingPeriod = row.AccountingPeriod;
        InvoicePeriod = row.InvoicePeriod;
        ReservationCode = row.ReservationCode;
        PropertyCode = row.PropertyCode;
        ContactName = row.ContactName;
        TotalAmount = row.TotalAmount;
        PaidAmount = row.PaidAmount;
        BalanceDue = row.BalanceDue;
        InvoiceStatus = row.InvoiceStatus;
        IsActive = row.IsActive;
        Notes = row.Notes;
    }
}

public class TransactionChainInvoiceLineDto
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

    public TransactionChainInvoiceLineDto()
    {
    }

    public TransactionChainInvoiceLineDto(TransactionChainInvoiceLine row)
    {
        OfficeName = row.OfficeName;
        InvoiceCode = row.InvoiceCode;
        ReservationCode = row.ReservationCode;
        PropertyCode = row.PropertyCode;
        ContactName = row.ContactName;
        LineNumber = row.LineNumber;
        LedgerLineDate = row.LedgerLineDate;
        LineDescription = row.LineDescription;
        LineAmount = row.LineAmount;
        CostCode = row.CostCode;
        LineType = row.LineType;
        PaymentCode = row.PaymentCode;
        PaymentDate = row.PaymentDate;
        PaymentAmount = row.PaymentAmount;
    }
}

public class TransactionChainPaymentDto
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

    public TransactionChainPaymentDto()
    {
    }

    public TransactionChainPaymentDto(TransactionChainPayment row)
    {
        OfficeName = row.OfficeName;
        PaymentCode = row.PaymentCode;
        PaymentDate = row.PaymentDate;
        Amount = row.Amount;
        PaymentDescription = row.PaymentDescription;
        PaymentType = row.PaymentType;
        CostCode = row.CostCode;
        PaymentStatus = row.PaymentStatus;
        IsActive = row.IsActive;
        DepositCode = row.DepositCode;
        DepositDate = row.DepositDate;
        DepositAmount = row.DepositAmount;
        TransferCode = row.TransferCode;
        InvoiceCodes = row.InvoiceCodes;
        InvoiceCount = row.InvoiceCount;
    }
}

public class TransactionChainDepositDto
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

    public TransactionChainDepositDto()
    {
    }

    public TransactionChainDepositDto(TransactionChainDeposit row)
    {
        OfficeName = row.OfficeName;
        DepositCode = row.DepositCode;
        DepositDate = row.DepositDate;
        AccountingPeriod = row.AccountingPeriod;
        Amount = row.Amount;
        DepositDescription = row.DepositDescription;
        BankAccount = row.BankAccount;
        DepositStatus = row.DepositStatus;
        IsActive = row.IsActive;
        TransferCode = row.TransferCode;
        TransferDate = row.TransferDate;
        TransferAmount = row.TransferAmount;
        PaymentCodes = row.PaymentCodes;
        PaymentCount = row.PaymentCount;
        PaymentTotal = row.PaymentTotal;
    }
}

public class TransactionChainTransferDto
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

    public TransactionChainTransferDto()
    {
    }

    public TransactionChainTransferDto(TransactionChainTransfer row)
    {
        OfficeName = row.OfficeName;
        TransferCode = row.TransferCode;
        TransferDate = row.TransferDate;
        AccountingPeriod = row.AccountingPeriod;
        Amount = row.Amount;
        TransferDescription = row.TransferDescription;
        BankAccount = row.BankAccount;
        TransferStatus = row.TransferStatus;
        TransferCompleted = row.TransferCompleted;
        IsActive = row.IsActive;
        DepositCodes = row.DepositCodes;
        DepositCount = row.DepositCount;
        DepositTotal = row.DepositTotal;
        SplitCount = row.SplitCount;
    }
}

public class TransactionChainSummaryDto
{
    public string OfficeNames { get; set; } = string.Empty;
    public DateOnly? StartDateFilter { get; set; }
    public DateOnly? EndDateFilter { get; set; }
    public int InvoiceCount { get; set; }
    public int PaymentCount { get; set; }
    public int DepositCount { get; set; }
    public int TransferCount { get; set; }

    public TransactionChainSummaryDto()
    {
    }

    public TransactionChainSummaryDto(TransactionChainSummary summary)
    {
        OfficeNames = summary.OfficeNames;
        StartDateFilter = summary.StartDateFilter;
        EndDateFilter = summary.EndDateFilter;
        InvoiceCount = summary.InvoiceCount;
        PaymentCount = summary.PaymentCount;
        DepositCount = summary.DepositCount;
        TransferCount = summary.TransferCount;
    }
}
