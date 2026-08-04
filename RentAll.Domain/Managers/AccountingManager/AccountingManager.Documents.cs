using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Invoice

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, Guid currentUser)
    {
        var created = await _accountingRepository.CreateAsync(invoice);
        await CreateJournalEntryFromInvoiceAsync(created, currentUser);
        return created;
    }

    public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
    {
        var existingInvoice = await _accountingRepository.GetInvoiceByIdAsync(invoice.InvoiceId, invoice.OrganizationId);
        if (existingInvoice == null)
            throw new Exception("Invoice not found");

        var updatedInvoice = await _accountingRepository.UpdateByIdAsync(invoice);
        await TryReplaceJournalEntriesFromInvoiceAsync(updatedInvoice, existingInvoice);
        return updatedInvoice;
    }

    public async Task DeleteInvoiceAsync(Guid invoiceId, Guid organizationId)
    {
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId is required.", nameof(invoiceId));

        var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, organizationId);
        if (invoice == null)
            throw new Exception("Invoice not found");

        if (invoice.PaidAmount != 0)
            throw new InvalidOperationException("Invoices with payments applied may not be deleted.");

        await DeleteJournalEntriesForInvoiceAsync(invoice);
        await _accountingRepository.DeleteInvoiceByIdAsync(invoiceId, organizationId);
    }

    public async Task DeleteInvoicesByReservationIdAsync(Guid organizationId, Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            throw new ArgumentException("ReservationId is required.", nameof(reservationId));

        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            ReservationId = reservationId,
            IncludeInactive = true,
            IncludePaid = true
        })).ToList();

        if (invoices.Any(invoice => invoice.PaidAmount != 0))
            throw new InvalidOperationException("This reservation has paid invoices applied to it. It may not be deleted.");

        foreach (var invoice in invoices)
            await DeleteInvoiceAsync(invoice.InvoiceId, organizationId);
    }

    #endregion

    #region Receipt

    public async Task<Receipt> CreateReceiptAsync(Receipt receipt, Guid currentUser)
    {
        var created = await _maintenanceRepository.CreateReceiptAsync(receipt);

        if (created.BankCardId == null)
            await CreateJournalEntryFromBillAsync(created, currentUser);
        else
            await CreateJournalEntryFromReceiptAsync(created, currentUser);

        return created;
    }

    public async Task<Receipt> UpdateBillAsync(Receipt bill, Guid currentUser)
    {
        var existingBill = bill.ReceiptId != Guid.Empty
            ? await _maintenanceRepository.GetReceiptByIdAsync(bill.ReceiptId, bill.OrganizationId)
            : null;

        await _maintenanceRepository.UpdateReceiptAsync(bill);
        var freshBill = await _maintenanceRepository.GetReceiptByIdAsync(bill.ReceiptId, bill.OrganizationId)
            ?? throw new Exception("Bill not found after update");

        if (existingBill?.BankCardId is > 0)
            await DeleteJournalEntriesForReceiptAsync(existingBill);

        await ReplaceJournalEntriesFromBillAsync(freshBill, currentUser);
        return freshBill;
    }

    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt, Guid currentUser)
    {
        var existingReceipt = receipt.ReceiptId != Guid.Empty
            ? await _maintenanceRepository.GetReceiptByIdAsync(receipt.ReceiptId, receipt.OrganizationId)
            : null;

        var updatedReceipt = await _maintenanceRepository.UpdateReceiptAsync(receipt);
        var freshReceipt = await _maintenanceRepository.GetReceiptByIdAsync(updatedReceipt.ReceiptId, updatedReceipt.OrganizationId)
            ?? throw new Exception("Receipt not found after update");

        if (existingReceipt != null && existingReceipt.BankCardId is not > 0)
            await DeleteJournalEntriesForBillAsync(existingReceipt);

        await ReplaceJournalEntriesFromReceiptAsync(freshReceipt, currentUser);
        return freshReceipt;
    }

    public async Task DeleteReceiptAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        if (receiptId == Guid.Empty)
            throw new ArgumentException("ReceiptId is required.", nameof(receiptId));

        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, organizationId);
        if (receipt == null)
            throw new Exception("Receipt record not found");

        if (receipt.BankCardId == null)
            await DeleteJournalEntriesForBillAsync(receipt);
        else
            await DeleteJournalEntriesForReceiptAsync(receipt);

        await _maintenanceRepository.DeleteReceiptByIdAsync(receiptId, organizationId, currentUser);
    }

    #endregion

    #region Work Order

    public async Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder, Guid currentUser)
    {
        var created = await _maintenanceRepository.CreateWorkOrderAsync(workOrder);
        await CreateJournalEntryFromWorkOrderAsync(created, currentUser);
        return created;
    }

    public async Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder, Guid currentUser)
    {
        var updatedWorkOrder = await _maintenanceRepository.UpdateWorkOrderAsync(workOrder);
        var freshWorkOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(updatedWorkOrder.WorkOrderId, updatedWorkOrder.OrganizationId)
            ?? throw new Exception("Work order not found after update");

        await TryReplaceJournalEntriesFromWorkOrderAsync(freshWorkOrder, currentUser);
        return freshWorkOrder;
    }

    public async Task DeleteWorkOrderAsync(Guid workOrderId, Guid organizationId, Guid currentUser)
    {
        if (workOrderId == Guid.Empty)
            throw new ArgumentException("WorkOrderId is required.", nameof(workOrderId));

        var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, organizationId);
        if (workOrder == null)
            throw new Exception("Work order record not found");

        await DeleteJournalEntriesForWorkOrderAsync(workOrder);
        await _maintenanceRepository.DeleteWorkOrderByIdAsync(workOrderId, organizationId, currentUser);
    }

    #endregion

    #region Payment

    public async Task<Payment> CreatePaymentAsync(Payment payment, Guid currentUser)
    {
        var created = await _accountingRepository.CreatePaymentAsync(payment);
        return await _accountingRepository.GetPaymentByIdAsync(created.PaymentId, created.OrganizationId)
            ?? created;
    }

    public Task<Payment> CreatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
        => ApplyInvoicePaymentAsync(payment, null, allocations, officeAccess, currentUser);

    public async Task<Payment> UpdatePaymentAsync(Payment payment, Guid currentUser)
    {
        var updated = await _accountingRepository.UpdatePaymentAsync(payment);
        return await _accountingRepository.GetPaymentByIdAsync(updated.PaymentId, updated.OrganizationId)
            ?? updated;
    }

    public Task<Payment> UpdatePaymentWithInvoiceAllocationsAsync(Payment payment, IReadOnlyList<PaymentInvoiceAllocation> allocations, string officeAccess, Guid currentUser)
        => UpdateInvoicePaymentWithExplicitAllocationsAsync(payment, allocations, officeAccess, currentUser);

    public async Task DeletePaymentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("PaymentId is required.", nameof(paymentId));

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            throw new Exception("Payment record not found");

        var paymentLedgerLines = await _accountingRepository.GetLedgerLinesByPaymentIdAsync(paymentId, organizationId);

        await DeleteJournalEntriesForPaymentAsync(payment);

        foreach (var invoiceGroup in paymentLedgerLines.GroupBy(line => line.InvoiceId))
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceGroup.Key, organizationId);
            if (invoice == null)
                continue;

            foreach (var paymentLine in invoiceGroup)
            {
                await DeleteJournalEntriesForInvoicePaymentLedgerLineAsync(invoice, ToInvoicePaymentLedgerLine(paymentLine));
                invoice.PaidAmount -= paymentLine.Amount;
                invoice.LedgerLines.RemoveAll(line => line.LedgerLineId == paymentLine.LedgerLineId);
            }

            invoice.ModifiedBy = currentUser;
            var updatedInvoice = await _accountingRepository.UpdateByIdAsync(invoice);
            await PruneOrphanedInvoicePaymentJournalEntriesAsync(updatedInvoice, await GetActiveInvoicePaymentLedgerLinesAsync(updatedInvoice));
        }

        await _accountingRepository.DeletePaymentByIdAsync(paymentId, organizationId, currentUser);
    }

    #endregion

    #region Deposit

    public async Task<Deposit> CreateDepositAsync(Deposit deposit, Guid currentUser)
    {
        await PrepareDepositForSaveAsync(deposit);
        var created = await _accountingRepository.CreateDepositAsync(deposit);
        await CreateJournalEntryFromDepositAsync(created, currentUser);
        return created;
    }

    public async Task<Deposit> UpdateDepositAsync(Deposit deposit, Guid currentUser)
    {
        var existing = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, deposit.OrganizationId)
            ?? throw new Exception("Deposit not found");

        deposit.CreatedBy = existing.CreatedBy;
        deposit.DepositCode = existing.DepositCode;

        await PrepareDepositForSaveAsync(deposit);
        await _accountingRepository.UpdateDepositAsync(deposit);

        var freshDeposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, deposit.OrganizationId)
            ?? throw new Exception("Deposit not found after update");

        await TryReplaceJournalEntriesFromDepositAsync(freshDeposit, currentUser);

        return await _accountingRepository.GetDepositByIdAsync(freshDeposit.DepositId, freshDeposit.OrganizationId)
            ?? freshDeposit;
    }

    public async Task DeleteDepositAsync(Guid depositId, Guid organizationId, Guid currentUser)
    {
        if (depositId == Guid.Empty)
            throw new ArgumentException("DepositId is required.", nameof(depositId));

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
            throw new Exception("Deposit record not found");

        await DeleteJournalEntriesForDepositAsync(deposit);
        await _accountingRepository.DeleteDepositByIdAsync(depositId, organizationId, currentUser);
    }

    #endregion

    #region Transfer

    public async Task<Transfer> CreateTransferAsync(Transfer transfer, Guid currentUser)
    {
        await PrepareTransferForSaveAsync(transfer);
        var created = await _accountingRepository.CreateTransferAsync(transfer);
        await CreateJournalEntryFromTransferAsync(created, currentUser);
        return created;
    }

    public async Task<Transfer> UpdateTransferAsync(Transfer transfer, Guid currentUser)
    {
        var existing = await _accountingRepository.GetTransferByIdAsync(transfer.TransferId, transfer.OrganizationId)
            ?? throw new Exception("Transfer not found");

        transfer.CreatedBy = existing.CreatedBy;
        transfer.TransferCode = existing.TransferCode;
        transfer.HasBeenTransfered = existing.HasBeenTransfered;

        await PrepareTransferForSaveAsync(transfer);
        await _accountingRepository.UpdateTransferAsync(transfer);

        var freshTransfer = await _accountingRepository.GetTransferByIdAsync(transfer.TransferId, transfer.OrganizationId)
            ?? throw new Exception("Transfer not found after update");

        await TryReplaceJournalEntriesFromTransferAsync(freshTransfer, currentUser);

        return await _accountingRepository.GetTransferByIdAsync(freshTransfer.TransferId, freshTransfer.OrganizationId)
            ?? freshTransfer;
    }

    public async Task<Transfer> PostTransferReportAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        if (transferId == Guid.Empty)
            throw new ArgumentException("TransferId is required");

        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found");

        if (!transfer.IsActive)
            throw new Exception("Transfer is inactive");

        if (transfer.PostingStatusId == (int)PostingStatus.HardClosed)
            throw new Exception("Transfer is hard closed and cannot be posted");

        if (transfer.PostingStatusId == (int)PostingStatus.SoftClosed)
            throw new Exception("Transfer is soft closed and cannot be posted");

        if (transfer.HasBeenTransfered)
            throw new Exception("Transfer has already been transfered");

        ValidateTransferForJournalEntry(transfer);

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(transfer.OrganizationId, transfer.OfficeId);
        var escrowDepositAccountId = GetDefaultEscrowDepositAccount(chartOfAccounts, transfer.OfficeId, accountingOffice);
        if (escrowDepositAccountId <= 0)
            throw new Exception("Default escrow deposit account is not configured for this office");

        transfer.BankAccountId = escrowDepositAccountId;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);

        var refreshedTransfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found after update");

        await TryReplaceJournalEntriesFromTransferAsync(refreshedTransfer, currentUser);

        refreshedTransfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? throw new Exception("Transfer not found after journal entry refresh");

        var transferJournalEntries = await GetJournalEntriesForSourceAsync(
            refreshedTransfer.OrganizationId,
            refreshedTransfer.OfficeId,
            SourceType.Transfer,
            transferId);
        if (!transferJournalEntries.Any(entry => entry.JournalEntryId != Guid.Empty))
            throw new Exception("Unable to create transfer journal entry");

        refreshedTransfer.HasBeenTransfered = true;
        refreshedTransfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(refreshedTransfer);

        return await _accountingRepository.GetTransferByIdAsync(transferId, organizationId)
            ?? refreshedTransfer;
    }

    public async Task DeleteTransferAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        if (transferId == Guid.Empty)
            throw new ArgumentException("TransferId is required.", nameof(transferId));

        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null)
            throw new Exception("Transfer record not found");

        await DeleteJournalEntriesForTransferAsync(transfer);
        await _accountingRepository.DeleteTransferByIdAsync(transferId, organizationId, currentUser);
    }

    #endregion
}
