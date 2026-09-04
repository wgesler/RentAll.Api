using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntrySyncResult> SyncJournalEntriesForHealthFixAsync(
        Guid organizationId,
        string officeIds,
        string syncType,
        IReadOnlyList<Guid> documentIds,
        int? paymentKindId,
        Guid currentUser,
        IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var distinctIds = documentIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var result = new JournalEntrySyncResult();
        var total = distinctIds.Count;
        var processed = 0;
        ReportSyncProgress(progress, syncType, total, processed, result, total == 0 ? "Completed" : "Running");

        foreach (var documentId in distinctIds)
        {
            result.DocumentsProcessed++;

            try
            {
                switch (syncType)
                {
                    case "receipt":
                        await SyncReceiptForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    case "bill":
                        await SyncBillForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    case "workOrder":
                        await SyncWorkOrderForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    case "invoice":
                        await SyncInvoiceForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    case "payment":
                        await SyncPaymentForHealthFixAsync(organizationId, documentId, paymentKindId, currentUser, result);
                        break;
                    case "deposit":
                        await SyncDepositForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    case "transfer":
                        await SyncTransferForHealthFixAsync(organizationId, documentId, currentUser, result);
                        break;
                    default:
                        throw new Exception($"Sync type '{syncType}' is not supported for health fix.");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{documentId}: {ex.Message}");
            }

            processed++;
            ReportSyncProgress(progress, syncType, total, processed, result, processed >= total ? "Completed" : "Running");
        }

        return result;
    }

    async Task SyncReceiptForHealthFixAsync(Guid organizationId, Guid receiptId, Guid currentUser, JournalEntrySyncResult result)
    {
        var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptId, organizationId);
        if (receipt == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        EnsureReceiptIsCardReceipt(receipt);

        var hadJournalEntry = await ReceiptHasLinkedJournalEntryAsync(
            receipt.OrganizationId,
            receipt.OfficeId,
            receipt.ReceiptId);

        await ReplaceJournalEntriesFromReceiptAsync(receipt, currentUser);

        var hasJournalEntry = await ReceiptHasLinkedJournalEntryAsync(
            receipt.OrganizationId,
            receipt.OfficeId,
            receipt.ReceiptId);

        if (hasJournalEntry && !hadJournalEntry)
            result.JournalEntriesCreated++;
        else if (hasJournalEntry)
            result.JournalEntriesSkipped++;
        else
            result.Errors.Add($"Receipt {receipt.ReceiptCode}: no journal entry after fix.");
    }

    async Task SyncBillForHealthFixAsync(Guid organizationId, Guid billId, Guid currentUser, JournalEntrySyncResult result)
    {
        var bill = await _maintenanceRepository.GetReceiptByIdAsync(billId, organizationId);
        if (bill == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        EnsureReceiptIsBill(bill);
        bill = await LoadReceiptWithSplitsAsync(bill);

        var expectJournalEntry = ShouldExpectBillJournalEntry(bill);
        if (!expectJournalEntry)
        {
            await ReplaceJournalEntriesFromBillAsync(bill, currentUser);
            result.JournalEntriesSkipped++;
            return;
        }

        var hadJournalEntry = await BillHasBillJournalEntryAsync(bill.OrganizationId, bill.OfficeId, bill.ReceiptId);
        await ReplaceJournalEntriesFromBillAsync(bill, currentUser);
        var hasJournalEntry = await BillHasBillJournalEntryAsync(bill.OrganizationId, bill.OfficeId, bill.ReceiptId);

        if (hasJournalEntry && !hadJournalEntry)
            result.JournalEntriesCreated++;
        else if (hasJournalEntry)
            result.JournalEntriesSkipped++;
        else
        {
            var billLabel = !string.IsNullOrWhiteSpace(bill.BillNumber) ? bill.BillNumber : bill.ReceiptCode;
            result.Errors.Add($"Bill {billLabel}: no bill journal entry after fix.");
        }
    }

    async Task SyncWorkOrderForHealthFixAsync(Guid organizationId, Guid workOrderId, Guid currentUser, JournalEntrySyncResult result)
    {
        var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderId, organizationId);
        if (workOrder == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var expectJournalEntry = ShouldExpectWorkOrderJournalEntry(workOrder);
        if (!expectJournalEntry)
        {
            await TryReplaceJournalEntriesFromWorkOrderAsync(workOrder, currentUser);
            result.JournalEntriesSkipped++;
            return;
        }

        var hadJournalEntry = await WorkOrderHasLinkedJournalEntryAsync(
            workOrder.OrganizationId,
            workOrder.OfficeId,
            workOrder.WorkOrderId);

        await TryReplaceJournalEntriesFromWorkOrderAsync(workOrder, currentUser);

        var hasJournalEntry = await WorkOrderHasLinkedJournalEntryAsync(
            workOrder.OrganizationId,
            workOrder.OfficeId,
            workOrder.WorkOrderId);

        if (hasJournalEntry && !hadJournalEntry)
            result.JournalEntriesCreated++;
        else if (hasJournalEntry)
            result.JournalEntriesSkipped++;
        else
            result.Errors.Add($"Work order {workOrder.WorkOrderCode}: no journal entry after fix.");
    }

    async Task SyncInvoiceForHealthFixAsync(Guid organizationId, Guid invoiceId, Guid currentUser, JournalEntrySyncResult result)
    {
        var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceId, organizationId);
        if (invoice == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var hadChargeJournalEntry = await InvoiceHasChargeJournalEntryAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            invoice.InvoiceId);

        var refreshError = await RefreshInvoiceChargeJournalEntriesAsync(invoice, currentUser);

        var hasChargeJournalEntry = await InvoiceHasChargeJournalEntryAsync(
            invoice.OrganizationId,
            invoice.OfficeId,
            invoice.InvoiceId);

        if (hasChargeJournalEntry && !hadChargeJournalEntry)
            result.JournalEntriesCreated++;
        else if (hasChargeJournalEntry)
            result.JournalEntriesSkipped++;
        else if (invoice.TotalAmount != 0)
        {
            var message = refreshError != null
                ? $"Invoice {invoice.InvoiceCode}: {refreshError}"
                : $"Invoice {invoice.InvoiceCode}: no charge journal entry after fix.";
            result.Errors.Add(message);
        }
        else
        {
            result.JournalEntriesSkipped++;
        }
    }

    async Task SyncPaymentForHealthFixAsync(
        Guid organizationId,
        Guid paymentId,
        int? paymentKindId,
        Guid currentUser,
        JournalEntrySyncResult result)
    {
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        if (paymentKindId.HasValue && payment.PaymentKindId != paymentKindId.Value)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        switch ((PaymentKind)payment.PaymentKindId)
        {
            case PaymentKind.Invoice:
                await SyncInvoicePaymentForHealthFixAsync(payment, organizationId, currentUser, result);
                break;
            case PaymentKind.Bill:
                await SyncBillPaymentJournalEntryAsync(payment, organizationId, currentUser, result);
                break;
            case PaymentKind.Owner:
                await SyncOwnerPaymentJournalEntryAsync(payment, organizationId, currentUser, result);
                break;
            default:
                result.JournalEntriesSkipped++;
                break;
        }
    }

    async Task SyncInvoicePaymentForHealthFixAsync(Payment paymentSummary, Guid organizationId, Guid currentUser, JournalEntrySyncResult result)
    {
        await CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(
            paymentSummary.PaymentId,
            organizationId,
            currentUser,
            allowPartialAllocationsOnMismatch: true);

        if (await PaymentHasHealthPaymentJournalEntryAsync(paymentSummary.PaymentId, organizationId))
        {
            result.JournalEntriesCreated++;
            return;
        }

        result.JournalEntriesSkipped++;
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
        result.Errors.Add($"{ResolvePaymentDocumentCode(payment, paymentSummary)}: no Health Payment JE after fix.");
    }

    async Task SyncDepositForHealthFixAsync(Guid organizationId, Guid depositId, Guid currentUser, JournalEntrySyncResult result)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var depositLabel = string.IsNullOrWhiteSpace(deposit.DepositCode)
            ? deposit.DepositId.ToString()
            : deposit.DepositCode.Trim();
        var trail = new AccountingSyncBailTrail();

        var originalSplitLineIds = (deposit.Splits ?? [])
            .Select(split => split.JournalEntryLineId)
            .ToList();
        await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit, trail);
        if (DepositSplitJournalEntryLineIdsChanged(originalSplitLineIds, deposit.Splits))
        {
            deposit.ModifiedBy = currentUser;
            var updated = await _accountingRepository.UpdateDepositAsync(deposit);
            deposit.Splits = updated.Splits;
        }

        await TryReplaceJournalEntriesFromDepositWithDiagnosticsAsync(deposit, currentUser, trail);

        if (await DepositHasHealthJournalEntryAsync(organizationId, deposit.OfficeId, deposit.DepositId))
            result.JournalEntriesCreated++;
        else
        {
            result.JournalEntriesSkipped++;
            result.Errors.Add($"Deposit {depositLabel}: no deposit JE after fix.");
        }
    }

    async Task SyncTransferForHealthFixAsync(Guid organizationId, Guid transferId, Guid currentUser, JournalEntrySyncResult result)
    {
        var transfer = await _accountingRepository.GetTransferByIdAsync(transferId, organizationId);
        if (transfer == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var transferLabel = string.IsNullOrWhiteSpace(transfer.TransferCode)
            ? transfer.TransferId.ToString()
            : transfer.TransferCode.Trim();
        var trail = new AccountingSyncBailTrail();

        var originalSplitLineIds = (transfer.Splits ?? [])
            .Select(split => split.JournalEntryLineId)
            .ToList();
        await ReconcileTransferSplitJournalEntryLineIdsAsync(transfer, trail);
        if (TransferSplitJournalEntryLineIdsChanged(originalSplitLineIds, transfer.Splits))
        {
            transfer.ModifiedBy = currentUser;
            var updated = await _accountingRepository.UpdateTransferAsync(transfer);
            transfer.Splits = updated.Splits;
        }

        await TryReplaceJournalEntriesFromTransferWithDiagnosticsAsync(transfer, currentUser, trail);

        if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
            result.JournalEntriesCreated++;
        else
        {
            result.JournalEntriesSkipped++;
            result.Errors.Add($"Transfer {transferLabel}: no transfer JE after fix.");
        }
    }
}
