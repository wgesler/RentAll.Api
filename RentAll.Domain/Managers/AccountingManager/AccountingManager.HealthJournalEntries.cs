using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Health Journal Entries
    public Task<JournalEntrySyncResult> SyncJournalEntriesForHealthFixAsync(Guid organizationId, string officeIds, string syncType, IReadOnlyList<Guid> documentIds, int? paymentKindId, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
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

    private async Task<JournalEntrySyncResult> RunHealthFixForDocumentsAsync(Guid organizationId, string officeIds, string syncType, IReadOnlyList<Guid> documentIds, int? paymentKindId, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress)
    {
        var scannedIds = await ResolveBrokenDocumentIdsFromHealthScanAsync(organizationId, officeIds, syncType, paymentKindId);
        var distinctIds = documentIds.Count > 0
            ? documentIds.Where(id => id != Guid.Empty).Concat(scannedIds).Distinct().ToList()
            : scannedIds;
        if (syncType == "deposit")
            distinctIds = await OrderDepositIdsForHealthFixAsync(organizationId, distinctIds);
        else if (syncType == "transfer")
        {
            await ClearTransferDepositDateViolationsForOfficeHealthFixAsync(organizationId, officeIds, currentUser);
        }

        var result = new JournalEntrySyncResult();
        if (distinctIds.Count == 0)
        {
            ReportSyncProgress(progress, syncType, 0, 0, result, "Completed");
            return result;
        }

        if (syncType != "deposit")
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

    private async Task<List<Guid>> ResolveBrokenDocumentIdsFromHealthScanAsync(Guid organizationId, string officeIds, string syncType, int? paymentKindId)
    {
        if (syncType == "deposit")
            return await ResolveDepositFixDocumentIdsAsync(organizationId, officeIds);

        if (syncType == "payment")
            return await ResolvePaymentFixDocumentIdsAsync(organizationId, officeIds, paymentKindId);

        if (syncType == "transfer")
            return await ResolveTransferFixDocumentIdsAsync(organizationId, officeIds);

        var scan = syncType switch
        {
            "receipt" => await _healthRepository.RunReceiptHealthCheckAsync(organizationId, officeIds),
            "bill" => await _healthRepository.RunBillHealthCheckAsync(organizationId, officeIds),
            "workOrder" => await _healthRepository.RunWorkOrderHealthCheckAsync(organizationId, officeIds),
            "invoice" => await _healthRepository.RunInvoiceHealthCheckAsync(organizationId, officeIds),
            _ => throw new Exception($"Sync type '{syncType}' is not supported for health fix scan.")
        };

        if (scan.Summary.IsClean)
            return [];

        return HealthDocumentTypeIdentification.CollectFixDocumentIds(scan.Issues).ToList();
    }

    private async Task<List<Guid>> ResolvePaymentFixDocumentIdsAsync(Guid organizationId, string officeIds, int? paymentKindId)
    {
        var ids = new HashSet<Guid>();

        var paymentScan = await _healthRepository.RunPaymentHealthCheckAsync(organizationId, officeIds, paymentKindId);
        if (!paymentScan.Summary.IsClean)
        {
            foreach (var id in HealthDocumentTypeIdentification.CollectPaymentFixIds(paymentScan.Issues))
                ids.Add(id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<List<Guid>> ResolveDepositFixDocumentIdsAsync(Guid organizationId, string officeIds)
    {
        var depositScan = await _healthRepository.RunDepositHealthCheckAsync(organizationId, officeIds);
        if (depositScan.Summary.IsClean)
            return [];

        return HealthDocumentTypeIdentification.CollectFixDocumentIds(depositScan.Issues).ToList();
    }

    private async Task<List<Guid>> ResolveTransferFixDocumentIdsAsync(Guid organizationId, string officeIds)
    {
        var transferScan = await _healthRepository.RunTransferHealthCheckAsync(organizationId, officeIds);
        if (transferScan.Summary.IsClean)
            return [];

        return HealthDocumentTypeIdentification.CollectFixDocumentIds(transferScan.Issues).ToList();
    }

    private async Task RunHealthFixBulkPruneAsync(string syncType, int? paymentKindId, Guid organizationId, string officeIds, IReadOnlyList<Guid> targetedDocumentIds, JournalEntrySyncResult result)
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

        payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            return;

        await SyncPaymentDocumentLinksAsync(payment, currentUser);
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

        if (paymentSummary.DepositId is { } stampedDepositId && stampedDepositId != Guid.Empty)
            payment.DepositId = stampedDepositId;

        if (await TryUnlinkInvoicePaymentFromInvalidDepositForHealthFixAsync(payment, organizationId, currentUser, result))
            payment = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, organizationId) ?? payment;

        var hadHealthPaymentJournalEntry = await PaymentHasHealthPaymentJournalEntryAsync(payment.PaymentId, organizationId);
        var hasDepositUfLine = payment.DepositId is { } depositId
            && depositId != Guid.Empty
            && await PaymentHasUndepositedFundsLineEqualToAmountAsync(payment);
        var needsPaymentJournalEntry = payment.DepositId is { } depositedId && depositedId != Guid.Empty
            ? !hasDepositUfLine
            : !hadHealthPaymentJournalEntry;
        if (forcePaymentJournalEntryUpsert || needsPaymentJournalEntry)
        {
            var createResult = await CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(
                payment.PaymentId,
                organizationId,
                currentUser,
                allowPartialAllocationsOnMismatch: true);

            payment = await _accountingRepository.GetPaymentByIdAsync(payment.PaymentId, organizationId) ?? payment;
            var paymentHealthyAfterFix = payment.DepositId is { } depositedPaymentId && depositedPaymentId != Guid.Empty
                ? await PaymentHasUndepositedFundsLineEqualToAmountAsync(payment)
                : await PaymentHasHealthPaymentJournalEntryAsync(payment.PaymentId, organizationId);

            if (paymentHealthyAfterFix)
            {
                if (hadHealthPaymentJournalEntry && !(payment.DepositId is { } depId && depId != Guid.Empty))
                    result.JournalEntriesSkipped++;
                else if (hadHealthPaymentJournalEntry && hasDepositUfLine)
                    result.JournalEntriesSkipped++;
                else
                    result.JournalEntriesCreated++;
            }
            else
            {
                result.JournalEntriesSkipped++;
                var definitiveError = MapPaymentJeCreateBailToAction(payment, createResult);
                result.Errors.Add(definitiveError);
                await LogHealthPaymentFixFailureAsync(
                    payment,
                    currentUser,
                    step: "PaymentJeCreate",
                    reason: createResult.FormatBailTrail(),
                    definitiveAction: definitiveError,
                    detail: payment.DepositId is { } missingUfDepositId && missingUfDepositId != Guid.Empty
                        ? "Deposited payment still missing UF JE coverage after create attempt."
                        : "Payment still missing Health Payment JE (kind 13/14 with PaymentId) after create attempt.");
            }
        }
        else if (payment.LedgerLines.Count == 0)
        {
            result.JournalEntriesSkipped++;
            var orphanAction = await ClassifyOrphanInvoicePaymentActionAsync(payment, organizationId)
                ?? BuildHealthPaymentFixError(payment.PaymentCode, "MANUAL REVIEW", "Orphan payment with no ledger lines.");
            result.Errors.Add(orphanAction);
            await LogHealthPaymentFixFailureAsync(
                payment,
                currentUser,
                step: "OrphanPayment",
                reason: "Payment header has no linked invoice ledger lines.",
                definitiveAction: orphanAction);
        }
        else
            result.JournalEntriesSkipped++;
    }

    async Task StampPaymentDepositIdsAfterSplitReconcileAsync(Deposit deposit, Guid organizationId, Guid currentUser)
    {
        var paymentIds = await CollectPaymentIdsToStampForDepositHealthFixAsync(deposit, organizationId);
        await SyncPaymentDepositIdsForDepositAsync(deposit, paymentIds, currentUser, unstampMissing: false);

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
        Guid currentUser,
        JournalEntrySyncResult result)
    {
        var trail = new AccountingSyncBailTrail();
        var deposit = payment.DepositId is { } depositId && depositId != Guid.Empty
            ? await _accountingRepository.GetDepositByIdAsync(depositId, payment.OrganizationId)
            : null;
        if (deposit != null)
        {
            await PruneWrongDepositSplitLinksForPaymentHealthFixAsync(payment, deposit, currentUser, trail);
            deposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId) ?? deposit;
        }

        deposit ??= payment.DepositId is { } missingDepositId && missingDepositId != Guid.Empty
            ? await _accountingRepository.GetDepositByIdAsync(missingDepositId, payment.OrganizationId)
            : null;
        if (deposit == null)
            return;

        var stillMissingSplitLink = !await PaymentHasDepositSplitLinkForHealthCheckAsync(payment, deposit);
        if (!stillMissingSplitLink)
            return;

        var definitiveError = MapDepositSplitReconcileBailToAction(payment, deposit, trail);
        result.Errors.Add(definitiveError);
        await LogHealthPaymentFixFailureAsync(
            payment,
            currentUser,
            step: "DepositSplitLink",
            reason: trail.FormatBailTrail(),
            definitiveAction: definitiveError,
            detail: $"Deposit={deposit.DepositCode}");
    }

    private async Task<bool> PaymentHasDepositSplitLinkForHealthCheckAsync(Payment payment, Deposit deposit)
    {
        if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
            return true;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(payment.OrganizationId, payment.OfficeId);
        var undepositedFundsAccountId = GetDefaultUndepositedFunds(chartOfAccounts, payment.OfficeId, accountingOffice);
        var accountsReceivableAccountId = GetDefaultAccountsReceivable(chartOfAccounts, payment.OfficeId, accountingOffice);
        if (undepositedFundsAccountId <= 0 || accountsReceivableAccountId <= 0)
            return false;

        var paymentEntries = await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(
            new JournalEntryGetByPaymentIdCriteria
            {
                OrganizationId = payment.OrganizationId,
                PaymentId = payment.PaymentId
            });

        foreach (var paymentEntry in paymentEntries)
        {
            if (!IsRematchableHealthInvoicePaymentJournalEntry(paymentEntry))
                continue;

            foreach (var line in paymentEntry.JournalEntryLines ?? [])
            {
                if (line.JournalEntryLineId == Guid.Empty || Math.Abs(line.Debit - line.Credit) <= 0.005m)
                    continue;

                if (line.ChartOfAccountId != accountsReceivableAccountId)
                    continue;

                if ((deposit.Splits ?? []).Any(split =>
                        split.JournalEntryLineId == line.JournalEntryLineId
                        && IsPaymentBackedDepositSplit(split, undepositedFundsAccountId)))
                    return true;
            }
        }

        return false;
    }

    async Task ResyncStampedPaymentsForDepositHealthFixAsync(
        Deposit deposit,
        Guid organizationId,
        Guid currentUser,
        JournalEntrySyncResult result)
    {
        var paymentIds = await CollectPaymentIdsToStampForDepositHealthFixAsync(deposit, organizationId);

        foreach (var paymentId in paymentIds)
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment == null || !payment.IsActive || payment.PaymentKindId != (int)PaymentKind.Invoice)
                continue;

            payment.DepositId = deposit.DepositId;
            await SyncInvoicePaymentForHealthFixAsync(payment, organizationId, currentUser, result);
        }

        var reloadedDeposit = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId);
        if (reloadedDeposit?.Splits != null)
            deposit.Splits = reloadedDeposit.Splits;
    }

    async Task SyncDepositForHealthFixAsync(Guid organizationId, Guid depositId, Guid currentUser, JournalEntrySyncResult result)
    {
        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        if (deposit == null)
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var trail = new AccountingSyncBailTrail();
        await RepairDepositForHealthFixAsync(deposit, organizationId, currentUser, result, trail);
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

        var unlinkedAmountMismatches = await UnlinkAmountMismatchedTransferSplitLinksForTransferHealthFixAsync(transfer, currentUser, trail);
        if (unlinkedAmountMismatches)
        {
            LogHealthTransferFixTrace(
                transfer,
                step: "UnlinkAmountMismatch",
                reason: trail.FormatBailTrail(),
                detail: "Cleared transfer split links where allocated amount <> escrow line amount.");
        }

        await TryStampDepositsForTransferHealthFixAsync(transfer, currentUser);

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

        foreach (var message in await GetUnresolvedTransferSplitMessagesAsync(transfer))
            result.Errors.Add(message);

        await SyncDepositTransferIdsForTransferAsync(transfer, currentUser);

        if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
        {
            await SyncTransferDocumentLinksAsync(transfer, currentUser);

            if (hadTransferJournalEntry)
                result.JournalEntriesSkipped++;
            else
                result.JournalEntriesCreated++;
            return;
        }

        await TryReplaceJournalEntriesFromTransferWithDiagnosticsAsync(transfer, currentUser, trail);

        if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
        {
            await SyncTransferDocumentLinksAsync(transfer, currentUser);

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
    #endregion
}
