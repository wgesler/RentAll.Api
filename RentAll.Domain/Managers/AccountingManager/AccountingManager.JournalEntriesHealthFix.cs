using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public Task<JournalEntrySyncResult> SyncJournalEntriesForHealthFixAsync(
        Guid organizationId,
        string officeIds,
        string syncType,
        IReadOnlyList<Guid> documentIds,
        int? paymentKindId,
        Guid currentUser,
        IProgress<JournalEntrySyncProgress>? progress = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (string.IsNullOrWhiteSpace(officeIds))
            throw new ArgumentException("OfficeIds is required.", nameof(officeIds));

        return WithOfficeSyncCacheAsync(organizationId, officeIds, () =>
            RunHealthFixForDocumentsAsync(
                organizationId,
                officeIds,
                syncType,
                documentIds,
                paymentKindId,
                currentUser,
                progress));
    }

    private async Task<JournalEntrySyncResult> RunHealthFixForDocumentsAsync(
        Guid organizationId,
        string officeIds,
        string syncType,
        IReadOnlyList<Guid> documentIds,
        int? paymentKindId,
        Guid currentUser,
        IProgress<JournalEntrySyncProgress>? progress)
    {
        // Always scan the office health-check proc first; issues drive the one-by-one repair list.
        var distinctIds = await ResolveBrokenDocumentIdsFromHealthScanAsync(organizationId, officeIds, syncType, paymentKindId);

        var result = new JournalEntrySyncResult();
        if (distinctIds.Count == 0)
        {
            ReportSyncProgress(progress, syncType, 0, 0, result, "Completed");
            return result;
        }

        await RunHealthFixBulkPruneAsync(syncType, paymentKindId, organizationId, officeIds, distinctIds, result);

        var total = distinctIds.Count;
        var processed = 0;
        ReportSyncProgress(progress, syncType, total, processed, result, "Running");

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

        if (syncType == "payment" && paymentKindId == (int)PaymentKind.Invoice)
        {
            await ReconcileOrphanPaymentsDuringSyncAsync(organizationId, officeIds, currentUser, result);

            var paymentScan = await _healthRepository.RunPaymentHealthCheckAsync(
                organizationId,
                officeIds,
                (int)PaymentKind.Invoice);
            await ReconcileDuplicateInvoicePaymentDocumentsForIssuesAsync(
                paymentScan.Issues,
                organizationId,
                currentUser,
                result);

            var documentLinksScan = await _healthRepository.RunDocumentLinksHealthCheckAsync(organizationId, officeIds);
            await ReconcileDuplicateInvoicePaymentDocumentsForIssuesAsync(
                documentLinksScan.Issues,
                organizationId,
                currentUser,
                result);
        }

        return result;
    }

    private async Task<List<Guid>> ResolveBrokenDocumentIdsFromHealthScanAsync(
        Guid organizationId,
        string officeIds,
        string syncType,
        int? paymentKindId)
    {
        var scan = syncType switch
        {
            "receipt" => await _healthRepository.RunReceiptHealthCheckAsync(organizationId, officeIds),
            "bill" => await _healthRepository.RunBillHealthCheckAsync(organizationId, officeIds),
            "workOrder" => await _healthRepository.RunWorkOrderHealthCheckAsync(organizationId, officeIds),
            "invoice" => await _healthRepository.RunInvoiceHealthCheckAsync(organizationId, officeIds),
            "payment" => await _healthRepository.RunPaymentHealthCheckAsync(organizationId, officeIds, paymentKindId),
            "deposit" => await _healthRepository.RunDepositHealthCheckAsync(organizationId, officeIds),
            "transfer" => await _healthRepository.RunTransferHealthCheckAsync(organizationId, officeIds),
            _ => throw new Exception($"Sync type '{syncType}' is not supported for health fix scan.")
        };

        if (scan.Summary.IsClean)
            return [];

        return syncType == "payment"
            ? DocumentHealthFixRouting.CollectPaymentFixIds(scan.Issues).ToList()
            : DocumentHealthFixRouting.CollectFixDocumentIds(scan.Issues).ToList();
    }

    private async Task RunHealthFixBulkPruneAsync(
        string syncType,
        int? paymentKindId,
        Guid organizationId,
        string officeIds,
        IReadOnlyList<Guid> targetedDocumentIds,
        JournalEntrySyncResult result)
    {
        if (targetedDocumentIds.Count == 0)
            return;

        var targetedIds = string.Join(",", targetedDocumentIds);

        if (syncType == "payment" && paymentKindId == (int)PaymentKind.Invoice)
        {
            result.JournalEntriesDeleted += await _journalEntryRepository.PruneDuplicateOpenInvoicePaymentJesAsync(
                organizationId,
                officeIds,
                paymentIds: targetedIds);
        }
        else if (syncType == "deposit")
        {
            result.JournalEntriesDeleted += await _journalEntryRepository.PruneDuplicateOpenDepositJesAsync(
                organizationId,
                officeIds,
                depositIds: targetedIds);
        }
    }

    private static DateOnly ResolveEffectiveAccountingPeriodForHealthFix(JournalEntry entry)
        => entry.AccountingPeriod > new DateOnly(1900, 1, 1) ? entry.AccountingPeriod : entry.TransactionDate;

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

        var openChargeEntries = (await GetAllJournalEntriesForInvoiceAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                invoice.InvoiceId))
            .Where(entry => entry.JournalEntryKindId == JournalEntryKind.Charge && entry.PostingStatusId == PostingStatus.Open)
            .ToList();

        foreach (var periodGroup in openChargeEntries.GroupBy(ResolveEffectiveAccountingPeriodForHealthFix))
        {
            if (periodGroup.Count() <= 1)
                continue;

            var workingEntries = periodGroup.ToList();
            await PruneOpenDuplicateAutoGeneratedJournalEntriesAsync(workingEntries, organizationId, currentUser);
        }

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

    async Task SyncDepositedInvoicePaymentsForDepositHealthFixAsync(
        Deposit deposit,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result)
    {
        var paymentIds = await CollectPaymentIdsForDepositHealthFixAsync(deposit, organizationId);

        foreach (var paymentId in paymentIds)
        {
            Payment? payment = null;
            if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                payment = cachedPayment;
            else
                payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);

            if (payment == null || !payment.IsActive || payment.PaymentKindId != (int)PaymentKind.Invoice)
                continue;

            await SyncInvoicePaymentForHealthFixAsync(payment, organizationId, currentUser, result);
        }

        var stampPaymentIds = await CollectPaymentIdsFromDepositSplitsAsync(deposit);
        await SyncPaymentDepositIdsForDepositAsync(deposit, stampPaymentIds, currentUser);

        if (_officeSyncCache != null)
        {
            foreach (var paymentId in stampPaymentIds)
            {
                if (_officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                    cachedPayment.DepositId = deposit.DepositId;
            }
        }

        var reloadedDeposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId);
        if (reloadedDeposit?.Splits != null)
            deposit.Splits = reloadedDeposit.Splits;
    }

    async Task SyncInvoicePaymentForHealthFixAsync(
        Payment paymentSummary,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result,
        bool forcePaymentJournalEntryUpsert = false)
    {
        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
        if (payment == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        result.JournalEntriesDeleted += await PruneDuplicateOpenInvoicePaymentJournalEntriesByPaymentIdAsync(
            payment.PaymentId,
            organizationId,
            currentUser);

        var hadHealthPaymentJournalEntry = await PaymentHasHealthPaymentJournalEntryAsync(payment.PaymentId, organizationId);
        if (forcePaymentJournalEntryUpsert || !hadHealthPaymentJournalEntry)
        {
            var createResult = await CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(
                payment.PaymentId,
                organizationId,
                currentUser,
                allowPartialAllocationsOnMismatch: true);

            if (await PaymentHasHealthPaymentJournalEntryAsync(payment.PaymentId, organizationId))
            {
                if (hadHealthPaymentJournalEntry)
                    result.JournalEntriesSkipped++;
                else
                    result.JournalEntriesCreated++;
            }
            else
            {
                result.JournalEntriesSkipped++;
                var paymentCode = ResolvePaymentDocumentCode(payment, paymentSummary);
                var bailTrail = createResult.FormatBailTrail().Trim();
                var detail = string.IsNullOrWhiteSpace(bailTrail)
                    ? "no Health Payment JE after fix."
                    : bailTrail;
                result.Errors.Add($"{paymentCode}: {detail}");
            }
        }
        else
        {
            result.JournalEntriesSkipped++;
        }

        payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
        if (payment != null)
            await ReconcileDepositSplitLinksForDepositedPaymentHealthFixAsync(payment, organizationId, currentUser);
    }

    async Task StampPaymentDepositIdsAfterSplitReconcileAsync(Deposit deposit, Guid organizationId, Guid currentUser)
    {
        var paymentIds = await CollectPaymentIdsFromDepositSplitsAsync(deposit);
        await SyncPaymentDepositIdsForDepositAsync(deposit, paymentIds, currentUser);

        if (_officeSyncCache == null)
            return;

        foreach (var paymentId in paymentIds)
        {
            if (_officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                cachedPayment.DepositId = deposit.DepositId;
        }
    }

    async Task ReconcileDepositSplitLinksForDepositedPaymentHealthFixAsync(
        Payment payment,
        Guid organizationId,
        Guid currentUser)
    {
        if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
            return;

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
            return;

        var trail = new AccountingSyncBailTrail();
        var originalSplitLineIds = (deposit.Splits ?? [])
            .Select(split => split.JournalEntryLineId)
            .ToList();
        await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit, trail);
        if (!DepositSplitJournalEntryLineIdsChanged(originalSplitLineIds, deposit.Splits))
            return;

        deposit.ModifiedBy = currentUser;
        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
        deposit.Splits = updated.Splits;
        _officeSyncCache?.ReplaceDeposit(deposit);
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
        var hadDepositJournalEntry = await DepositHasHealthJournalEntryAsync(organizationId, deposit.OfficeId, deposit.DepositId);

        result.JournalEntriesDeleted += await PruneDuplicateOpenDepositJournalEntriesByDepositIdAsync(
            depositId,
            organizationId,
            currentUser);

        var originalSplitLineIds = (deposit.Splits ?? [])
            .Select(split => split.JournalEntryLineId)
            .ToList();
        var originalSplitReservationIds = (deposit.Splits ?? [])
            .Select(split => split.ReservationId)
            .ToList();
        var originalSplitPropertyIds = (deposit.Splits ?? [])
            .Select(split => split.PropertyId)
            .ToList();
        await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit, trail);
        if (DepositSplitReconciliationChanged(
                originalSplitLineIds,
                originalSplitReservationIds,
                originalSplitPropertyIds,
                deposit.Splits))
        {
            deposit.ModifiedBy = currentUser;
            var updated = await _accountingRepository.UpdateDepositAsync(deposit);
            deposit.Splits = updated.Splits;
            _officeSyncCache?.ReplaceDeposit(deposit);
        }

        await StampPaymentDepositIdsAfterSplitReconcileAsync(deposit, organizationId, currentUser);

        if (await DepositHasHealthJournalEntryAsync(organizationId, deposit.OfficeId, deposit.DepositId))
        {
            if (hadDepositJournalEntry)
                result.JournalEntriesSkipped++;
            else
                result.JournalEntriesCreated++;
            return;
        }

        await TryReplaceJournalEntriesFromDepositWithDiagnosticsAsync(deposit, currentUser, trail);

        if (await DepositHasHealthJournalEntryAsync(organizationId, deposit.OfficeId, deposit.DepositId))
        {
            if (hadDepositJournalEntry)
                result.JournalEntriesSkipped++;
            else
                result.JournalEntriesCreated++;
        }
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
        var hadTransferJournalEntry = await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId);

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

        if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
        {
            if (hadTransferJournalEntry)
                result.JournalEntriesSkipped++;
            else
                result.JournalEntriesCreated++;
            return;
        }

        await TryReplaceJournalEntriesFromTransferWithDiagnosticsAsync(transfer, currentUser, trail);

        if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
        {
            if (hadTransferJournalEntry)
                result.JournalEntriesSkipped++;
            else
                result.JournalEntriesCreated++;
        }
        else
        {
            result.JournalEntriesSkipped++;
            result.Errors.Add($"Transfer {transferLabel}: no transfer JE after fix.");
        }
    }
}
