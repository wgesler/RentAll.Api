using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Health;

namespace RentAll.Infrastructure.Repositories.Health;

internal static class TransactionChainExportMapper
{
    public static TransactionChainExport Map(
        IEnumerable<TransactionChainRowEntity> chain,
        IEnumerable<TransactionChainInvoiceEntity> invoices,
        IEnumerable<TransactionChainInvoiceLineEntity> invoiceLines,
        IEnumerable<TransactionChainPaymentEntity> payments,
        IEnumerable<TransactionChainDepositEntity> deposits,
        IEnumerable<TransactionChainTransferEntity> transfers,
        TransactionChainSummaryEntity? summary)
        => new()
        {
            Chain = chain.Select(MapChainRow).ToList(),
            Invoices = invoices.Select(MapInvoice).ToList(),
            InvoiceLines = invoiceLines.Select(MapInvoiceLine).ToList(),
            Payments = payments.Select(MapPayment).ToList(),
            Deposits = deposits.Select(MapDeposit).ToList(),
            Transfers = transfers.Select(MapTransfer).ToList(),
            Summary = summary == null ? null : MapSummary(summary)
        };

    private static DateOnly? ToDateOnly(DateTime? value)
        => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static TransactionChainRow MapChainRow(TransactionChainRowEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            InvoiceCode = entity.InvoiceCode ?? string.Empty,
            InvoiceDate = ToDateOnly(entity.InvoiceDate),
            InvoiceAccountingPeriod = ToDateOnly(entity.InvoiceAccountingPeriod),
            ReservationCode = entity.ReservationCode,
            PropertyCode = entity.PropertyCode,
            ContactName = entity.ContactName,
            InvoiceTotal = entity.InvoiceTotal ?? 0m,
            InvoicePaid = entity.InvoicePaid ?? 0m,
            InvoiceBalance = entity.InvoiceBalance ?? 0m,
            InvoiceStatus = entity.InvoiceStatus,
            InvoiceIsActive = entity.InvoiceIsActive ?? false,
            LedgerLineNumber = entity.LedgerLineNumber ?? 0,
            LedgerLineDate = ToDateOnly(entity.LedgerLineDate),
            LedgerLineDescription = entity.LedgerLineDescription,
            LedgerLineAmount = entity.LedgerLineAmount ?? 0m,
            LedgerLineType = entity.LedgerLineType ?? string.Empty,
            LedgerCostCode = entity.LedgerCostCode,
            PaymentCode = entity.PaymentCode,
            PaymentDate = ToDateOnly(entity.PaymentDate),
            PaymentAmount = entity.PaymentAmount,
            PaymentDescription = entity.PaymentDescription,
            PaymentType = entity.PaymentType,
            PaymentCostCode = entity.PaymentCostCode,
            PaymentStatus = entity.PaymentStatus,
            PaymentIsActive = entity.PaymentIsActive,
            DepositCode = entity.DepositCode,
            DepositDate = ToDateOnly(entity.DepositDate),
            DepositAccountingPeriod = ToDateOnly(entity.DepositAccountingPeriod),
            DepositAmount = entity.DepositAmount,
            DepositDescription = entity.DepositDescription,
            DepositBankAccount = entity.DepositBankAccount,
            DepositStatus = entity.DepositStatus,
            DepositIsActive = entity.DepositIsActive,
            TransferCode = entity.TransferCode,
            TransferDate = ToDateOnly(entity.TransferDate),
            TransferAccountingPeriod = ToDateOnly(entity.TransferAccountingPeriod),
            TransferAmount = entity.TransferAmount,
            TransferDescription = entity.TransferDescription,
            TransferBankAccount = entity.TransferBankAccount,
            TransferStatus = entity.TransferStatus,
            TransferCompleted = entity.TransferCompleted,
            TransferIsActive = entity.TransferIsActive
        };

    private static TransactionChainInvoice MapInvoice(TransactionChainInvoiceEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            InvoiceCode = entity.InvoiceCode ?? string.Empty,
            InvoiceDate = ToDateOnly(entity.InvoiceDate) ?? default,
            DueDate = ToDateOnly(entity.DueDate) ?? default,
            AccountingPeriod = ToDateOnly(entity.AccountingPeriod) ?? default,
            InvoicePeriod = entity.InvoicePeriod,
            ReservationCode = entity.ReservationCode,
            PropertyCode = entity.PropertyCode,
            ContactName = entity.ContactName,
            TotalAmount = entity.TotalAmount ?? 0m,
            PaidAmount = entity.PaidAmount ?? 0m,
            BalanceDue = entity.BalanceDue ?? 0m,
            InvoiceStatus = entity.InvoiceStatus,
            IsActive = entity.IsActive ?? false,
            Notes = entity.Notes
        };

    private static TransactionChainInvoiceLine MapInvoiceLine(TransactionChainInvoiceLineEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            InvoiceCode = entity.InvoiceCode ?? string.Empty,
            ReservationCode = entity.ReservationCode,
            PropertyCode = entity.PropertyCode,
            ContactName = entity.ContactName,
            LineNumber = entity.LineNumber ?? 0,
            LedgerLineDate = ToDateOnly(entity.LedgerLineDate),
            LineDescription = entity.LineDescription,
            LineAmount = entity.LineAmount ?? 0m,
            CostCode = entity.CostCode,
            LineType = entity.LineType ?? string.Empty,
            PaymentCode = entity.PaymentCode,
            PaymentDate = ToDateOnly(entity.PaymentDate),
            PaymentAmount = entity.PaymentAmount
        };

    private static TransactionChainPayment MapPayment(TransactionChainPaymentEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            PaymentCode = entity.PaymentCode ?? string.Empty,
            PaymentDate = ToDateOnly(entity.PaymentDate) ?? default,
            Amount = entity.Amount ?? 0m,
            PaymentDescription = entity.PaymentDescription,
            PaymentType = entity.PaymentType,
            CostCode = entity.CostCode,
            PaymentStatus = entity.PaymentStatus,
            IsActive = entity.IsActive ?? false,
            DepositCode = entity.DepositCode,
            DepositDate = ToDateOnly(entity.DepositDate),
            DepositAmount = entity.DepositAmount,
            TransferCode = entity.TransferCode,
            InvoiceCodes = entity.InvoiceCodes,
            InvoiceCount = entity.InvoiceCount ?? 0
        };

    private static TransactionChainDeposit MapDeposit(TransactionChainDepositEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            DepositCode = entity.DepositCode ?? string.Empty,
            DepositDate = ToDateOnly(entity.DepositDate) ?? default,
            AccountingPeriod = ToDateOnly(entity.AccountingPeriod) ?? default,
            Amount = entity.Amount ?? 0m,
            DepositDescription = entity.DepositDescription,
            BankAccount = entity.BankAccount,
            DepositStatus = entity.DepositStatus,
            IsActive = entity.IsActive ?? false,
            TransferCode = entity.TransferCode,
            TransferDate = ToDateOnly(entity.TransferDate),
            TransferAmount = entity.TransferAmount,
            PaymentCodes = entity.PaymentCodes,
            PaymentCount = entity.PaymentCount ?? 0,
            PaymentTotal = entity.PaymentTotal
        };

    private static TransactionChainTransfer MapTransfer(TransactionChainTransferEntity entity)
        => new()
        {
            OfficeName = entity.OfficeName ?? string.Empty,
            TransferCode = entity.TransferCode ?? string.Empty,
            TransferDate = ToDateOnly(entity.TransferDate) ?? default,
            AccountingPeriod = ToDateOnly(entity.AccountingPeriod) ?? default,
            Amount = entity.Amount ?? 0m,
            TransferDescription = entity.TransferDescription,
            BankAccount = entity.BankAccount,
            TransferStatus = entity.TransferStatus,
            TransferCompleted = entity.TransferCompleted ?? false,
            IsActive = entity.IsActive ?? false,
            DepositCodes = entity.DepositCodes,
            DepositCount = entity.DepositCount ?? 0,
            DepositTotal = entity.DepositTotal,
            SplitCount = entity.SplitCount ?? 0
        };

    private static TransactionChainSummary MapSummary(TransactionChainSummaryEntity entity)
        => new()
        {
            OfficeNames = entity.OfficeNames ?? string.Empty,
            StartDateFilter = ToDateOnly(entity.StartDateFilter),
            EndDateFilter = ToDateOnly(entity.EndDateFilter),
            InvoiceCount = entity.InvoiceCount,
            PaymentCount = entity.PaymentCount,
            DepositCount = entity.DepositCount,
            TransferCount = entity.TransferCount
        };
}
