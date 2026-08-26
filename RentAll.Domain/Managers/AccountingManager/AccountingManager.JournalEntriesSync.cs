using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task<JournalEntrySyncResult> SyncInvoiceJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var result = new JournalEntrySyncResult();
            // Include inactive invoices in the work list and process them (UI Total matches).
            var invoices = _officeSyncCache!.InvoicesById.Values.ToList();

            var total = invoices.Count;
            var processed = 0;
            ReportSyncProgress(progress, "invoice", total, processed, result, "Running");

            foreach (var invoiceSummary in invoices)
            {
                result.DocumentsProcessed++;

                try
                {
                    var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId)
                        ?? invoiceSummary;

                    await TrackJournalEntryCreateAsync(
                        () => CreateJournalEntryFromInvoiceWithResultAsync(invoice, currentUser, processInactiveInvoice: true),
                        new JournalEntryGetCriteria
                        {
                            OrganizationId = invoice.OrganizationId,
                            OfficeIds = invoice.OfficeId.ToString(),
                            SourceTypeId = (int)SourceType.Invoice,
                            SourceId = invoice.InvoiceId,
                            IncludeUnposted = true
                        },
                        result);
                }
                catch (Exception ex)
                {
                    var message = $"Invoice {invoiceSummary.InvoiceCode}: {ex.Message}";
                    result.Errors.Add(message);
                    await LogAccountingErrorAsync(
                        trigger: "Invoice",
                        organizationId: organizationId,
                        officeId: invoiceSummary.OfficeId,
                        sourceTypeId: (int)SourceType.Invoice,
                        sourceId: invoiceSummary.InvoiceId,
                        documentCode: invoiceSummary.InvoiceCode,
                        accountingPeriod: null,
                        amount: invoiceSummary.TotalAmount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "invoice", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            if (total == 0)
                ReportSyncProgress(progress, "invoice", total, processed, result, "Completed");

            return result;
        });
    }

    public async Task<JournalEntrySyncResult> ClearInvoiceJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Invoice,
            (int)SourceType.InvoicePayment);
    }

    public async Task<JournalEntrySyncResult> SyncBillJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
        var result = new JournalEntrySyncResult();
        var outboundPayments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentDirection.Outbound)).ToList();
        var receiptIdsCoveredByPaymentDocuments = GetReceiptIdsCoveredByBillPaymentDocuments(outboundPayments);
        var bills = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Bill
        })).ToList();

        var total = bills.Count;
        var processed = 0;
        ReportSyncProgress(progress, "bill", total, processed, result, "Running");

        foreach (var billSummary in bills)
        {
            result.DocumentsProcessed++;

            try
            {
                var bill = await _maintenanceRepository.GetReceiptByIdAsync(billSummary.ReceiptId, organizationId);
                if (bill == null)
                    continue;

                EnsureReceiptIsBill(bill);

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromBillWithResultAsync(bill, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = bill.OrganizationId,
                        OfficeIds = bill.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.Bill,
                        SourceId = bill.ReceiptId,
                        IncludeUnposted = true
                    },
                    result);

                if (bill.PaidAmount != 0)
                {
                    if (receiptIdsCoveredByPaymentDocuments.Contains(bill.ReceiptId))
                    {
                        await DeleteLegacyBillPaymentJournalEntriesForReceiptAsync(bill);
                    }
                    else
                    {
                        try
                        {
                            await SyncBillPaymentJournalEntryAsync(bill, currentUser, result);
                        }
                        catch (Exception paymentEx)
                        {
                            var paymentBillLabel = !string.IsNullOrWhiteSpace(billSummary.BillNumber)
                                ? billSummary.BillNumber
                                : billSummary.ReceiptCode;
                            var message = $"Bill {paymentBillLabel} payment: {paymentEx.Message}";
                            result.Errors.Add(message);
                            await LogAccountingErrorAsync(
                                trigger: "BillPayment",
                                organizationId: organizationId,
                                officeId: billSummary.OfficeId,
                                sourceTypeId: (int)SourceType.BillPayment,
                                sourceId: billSummary.ReceiptId,
                                documentCode: paymentBillLabel,
                                accountingPeriod: null,
                                amount: billSummary.PaidAmount,
                                message: message,
                                currentUser: currentUser);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var billLabel = !string.IsNullOrWhiteSpace(billSummary.BillNumber)
                    ? billSummary.BillNumber
                    : billSummary.ReceiptCode;
                var message = $"Bill {billLabel}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Bill",
                    organizationId: organizationId,
                    officeId: billSummary.OfficeId,
                    sourceTypeId: (int)SourceType.Bill,
                    sourceId: billSummary.ReceiptId,
                    documentCode: billLabel,
                    accountingPeriod: null,
                    amount: billSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "bill", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "bill", total, processed, result, "Completed");

        return result;
        });
    }

    public async Task<JournalEntrySyncResult> ClearBillJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Bill,
            (int)SourceType.BillPayment);
    }

    public async Task<JournalEntrySyncResult> SyncReceiptJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
        var result = new JournalEntrySyncResult();
        var receipts = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            ReceiptKind = ReceiptKind.Card
        })).ToList();

        var total = receipts.Count;
        var processed = 0;
        ReportSyncProgress(progress, "receipt", total, processed, result, "Running");

        foreach (var receiptSummary in receipts)
        {
            result.DocumentsProcessed++;

            try
            {
                var receipt = await _maintenanceRepository.GetReceiptByIdAsync(receiptSummary.ReceiptId, organizationId);
                if (receipt == null)
                    continue;

                EnsureReceiptIsCardReceipt(receipt);

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromReceiptWithResultAsync(receipt, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = receipt.OrganizationId,
                        OfficeIds = receipt.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.Receipt,
                        SourceId = receipt.ReceiptId,
                        IncludeUnposted = true
                    },
                    result);
            }
            catch (Exception ex)
            {
                var message = $"Receipt {receiptSummary.ReceiptCode}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Receipt",
                    organizationId: organizationId,
                    officeId: receiptSummary.OfficeId,
                    sourceTypeId: (int)SourceType.Receipt,
                    sourceId: receiptSummary.ReceiptId,
                    documentCode: receiptSummary.ReceiptCode,
                    accountingPeriod: null,
                    amount: receiptSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "receipt", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "receipt", total, processed, result, "Completed");

        return result;
        });
    }

    public async Task<JournalEntrySyncResult> ClearReceiptJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        return await ClearJournalEntriesBySourceTypesAsync(
            organizationId,
            officeIds,
            (int)SourceType.Receipt);
    }

    public async Task<JournalEntrySyncResult> SyncWorkOrderJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
        var result = new JournalEntrySyncResult();
        var workOrders = (await _maintenanceRepository.GetWorkOrdersByCriteriaAsync(new WorkOrderGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds
        })).ToList();

        var total = workOrders.Count;
        var processed = 0;
        ReportSyncProgress(progress, "workOrder", total, processed, result, "Running");

        foreach (var workOrderSummary in workOrders)
        {
            result.DocumentsProcessed++;

            try
            {
                var workOrder = await _maintenanceRepository.GetWorkOrderByIdAsync(workOrderSummary.WorkOrderId, organizationId);
                if (workOrder == null)
                    continue;

                await TrackJournalEntryCreateAsync(
                    () => CreateJournalEntryFromWorkOrderWithResultAsync(workOrder, currentUser),
                    new JournalEntryGetCriteria
                    {
                        OrganizationId = workOrder.OrganizationId,
                        OfficeIds = workOrder.OfficeId.ToString(),
                        SourceTypeId = (int)SourceType.WorkOrder,
                        SourceId = workOrder.WorkOrderId,
                        IncludeUnposted = true
                    },
                    result);
            }
            catch (Exception ex)
            {
                var message = $"Work order {workOrderSummary.WorkOrderCode}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "WorkOrder",
                    organizationId: organizationId,
                    officeId: workOrderSummary.OfficeId,
                    sourceTypeId: (int)SourceType.WorkOrder,
                    sourceId: workOrderSummary.WorkOrderId,
                    documentCode: workOrderSummary.WorkOrderCode,
                    accountingPeriod: null,
                    amount: workOrderSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "workOrder", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        if (total == 0)
            ReportSyncProgress(progress, "workOrder", total, processed, result, "Completed");

        return result;
        });
    }

    public async Task<JournalEntrySyncResult> SyncDepositJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null, bool syncDocumentLinksAtEnd = true)
    {
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var result = new JournalEntrySyncResult();
            var total = deposits.Count;
            var processed = 0;
            ReportSyncProgress(progress, "deposit", total, processed, result, "Running");

            foreach (var deposit in deposits)
            {
                result.DocumentsProcessed++;
                var depositLabel = string.IsNullOrWhiteSpace(deposit.DepositCode)
                    ? deposit.DepositId.ToString()
                    : deposit.DepositCode.Trim();
                var trail = new AccountingSyncBailTrail();

                try
                {
                    trail.Note($"Sync deposit {depositLabel}");
                    var originalSplitLineIds = (deposit.Splits ?? [])
                        .Select(split => split.JournalEntryLineId)
                        .ToList();
                    await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit, trail);
                    if (DepositSplitJournalEntryLineIdsChanged(originalSplitLineIds, deposit.Splits))
                    {
                        deposit.ModifiedBy = currentUser;
                        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
                        deposit.Splits = updated.Splits;
                        _officeSyncCache?.ReplaceDeposit(deposit);
                        trail.Note("Deposit split JournalEntryLineIds changed — deposit saved.");
                    }
                    else
                    {
                        _officeSyncCache?.ReplaceDeposit(deposit);
                        trail.Note("Deposit split JournalEntryLineIds unchanged.");
                    }

                    // Payment DepositId stamp runs inside SyncDepositDocumentLinksAsync (via TryReplace).
                    await TryReplaceJournalEntriesFromDepositWithDiagnosticsAsync(deposit, currentUser, trail);

                    if (await DepositHasHealthJournalEntryAsync(organizationId, deposit.OfficeId, deposit.DepositId))
                    {
                        result.JournalEntriesCreated++;
                    }
                    else
                    {
                        result.JournalEntriesSkipped++;
                        trail.Bail("Health check: no SourceType.Deposit / Deposit-kind JE after sync.");
                        var message = $"Deposit {depositLabel}: no deposit JE after sync."
                            + Environment.NewLine + "Bail trail:" + Environment.NewLine + trail.FormatBailTrail();
                        result.Errors.Add($"{depositLabel}: no deposit JE. See DepositSkip log.");
                        await LogAccountingErrorAsync(
                            trigger: "DepositSkip",
                            organizationId: organizationId,
                            officeId: deposit.OfficeId,
                            sourceTypeId: (int)SourceType.Deposit,
                            sourceId: deposit.DepositId,
                            documentCode: depositLabel,
                            accountingPeriod: deposit.AccountingPeriod == default ? null : deposit.AccountingPeriod,
                            amount: deposit.Amount,
                            message: message,
                            currentUser: currentUser);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Deposit {depositLabel}: {ex.Message}"
                        + Environment.NewLine + "Bail trail:" + Environment.NewLine + trail.FormatBailTrail();
                    result.Errors.Add(message);
                    await LogAccountingErrorAsync(
                        trigger: "Deposit",
                        organizationId: organizationId,
                        officeId: deposit.OfficeId,
                        sourceTypeId: (int)SourceType.Deposit,
                        sourceId: deposit.DepositId,
                        documentCode: depositLabel,
                        accountingPeriod: deposit.AccountingPeriod == default ? null : deposit.AccountingPeriod,
                        amount: deposit.Amount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "deposit", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            if (total == 0)
                ReportSyncProgress(progress, "deposit", total, processed, result, "Completed");

            if (syncDocumentLinksAtEnd)
                await SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress);

            return result;
        }, depositsAlreadyLoaded: deposits);
    }

    public async Task<JournalEntrySyncResult> SyncTransferJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null, bool syncDocumentLinksAtEnd = true)
    {
        // Criteria already returns splits — avoid GetTransferById per row.
        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        var preloadResult = new JournalEntrySyncResult();
        ReportSyncProgress(progress, "transfer", transfers.Count, 0, preloadResult, "Loading office cache…");

        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var result = new JournalEntrySyncResult();
            var total = transfers.Count;
            var processed = 0;
            ReportSyncProgress(progress, "transfer", total, processed, result, "Running");

            foreach (var transfer in transfers)
            {
                result.DocumentsProcessed++;
                var transferLabel = string.IsNullOrWhiteSpace(transfer.TransferCode)
                    ? transfer.TransferId.ToString()
                    : transfer.TransferCode.Trim();
                var trail = new AccountingSyncBailTrail();

                try
                {
                    trail.Note($"Sync transfer {transferLabel}");
                    var originalSplitLineIds = (transfer.Splits ?? [])
                        .Select(split => split.JournalEntryLineId)
                        .ToList();
                    await ReconcileTransferSplitJournalEntryLineIdsAsync(transfer, trail);
                    if (TransferSplitJournalEntryLineIdsChanged(originalSplitLineIds, transfer.Splits))
                    {
                        transfer.ModifiedBy = currentUser;
                        var updated = await _accountingRepository.UpdateTransferAsync(transfer);
                        transfer.Splits = updated.Splits;
                        _officeSyncCache?.ReplaceTransfer(transfer);
                        trail.Note("Transfer split JournalEntryLineIds changed — transfer saved.");
                    }
                    else
                    {
                        _officeSyncCache?.ReplaceTransfer(transfer);
                        trail.Note("Transfer split JournalEntryLineIds unchanged.");
                    }

                    // Deposit TransferId stamp runs inside SyncTransferDocumentLinksAsync (via TryReplace).
                    await TryReplaceJournalEntriesFromTransferWithDiagnosticsAsync(transfer, currentUser, trail);

                    if (await TransferHasHealthJournalEntryAsync(organizationId, transfer.OfficeId, transfer.TransferId))
                    {
                        result.JournalEntriesCreated++;
                    }
                    else
                    {
                        result.JournalEntriesSkipped++;
                        trail.Bail("Health check: no SourceType.Transfer / Transfer-kind JE after sync.");
                        var message = $"Transfer {transferLabel}: no transfer JE after sync."
                            + Environment.NewLine + "Bail trail:" + Environment.NewLine + trail.FormatBailTrail();
                        result.Errors.Add($"{transferLabel}: no transfer JE. See TransferSkip log.");
                        await LogAccountingErrorAsync(
                            trigger: "TransferSkip",
                            organizationId: organizationId,
                            officeId: transfer.OfficeId,
                            sourceTypeId: (int)SourceType.Transfer,
                            sourceId: transfer.TransferId,
                            documentCode: transferLabel,
                            accountingPeriod: transfer.AccountingPeriod == default ? null : transfer.AccountingPeriod,
                            amount: transfer.Amount,
                            message: message,
                            currentUser: currentUser);
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Transfer {transferLabel}: {ex.Message}"
                        + Environment.NewLine + "Bail trail:" + Environment.NewLine + trail.FormatBailTrail();
                    result.Errors.Add(message);
                    await LogAccountingErrorAsync(
                        trigger: "Transfer",
                        organizationId: organizationId,
                        officeId: transfer.OfficeId,
                        sourceTypeId: (int)SourceType.Transfer,
                        sourceId: transfer.TransferId,
                        documentCode: transferLabel,
                        accountingPeriod: transfer.AccountingPeriod == default ? null : transfer.AccountingPeriod,
                        amount: transfer.Amount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "transfer", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            if (total == 0)
                ReportSyncProgress(progress, "transfer", total, processed, result, "Completed");

            if (syncDocumentLinksAtEnd)
                await SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress);

            return result;
        }, transfersAlreadyLoaded: transfers);
    }

    public async Task<JournalEntrySyncResult> RepairDepositAndTransferSplitLinksAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var result = new JournalEntrySyncResult();
            var total = deposits.Count + transfers.Count;
            var processed = 0;
            ReportSyncProgress(progress, "splitLinkRepair", total, processed, result, total == 0 ? "Completed" : "Running");

            foreach (var deposit in deposits)
            {
                result.DocumentsProcessed++;
                try
                {
                    var originalSplitLineIds = (deposit.Splits ?? [])
                        .Select(split => split.JournalEntryLineId)
                        .ToList();
                    await ReconcileDepositSplitJournalEntryLineIdsAsync(deposit);
                    if (DepositSplitJournalEntryLineIdsChanged(originalSplitLineIds, deposit.Splits))
                    {
                        deposit.ModifiedBy = currentUser;
                        var updated = await _accountingRepository.UpdateDepositAsync(deposit);
                        deposit.Splits = updated.Splits;
                        _officeSyncCache?.ReplaceDeposit(deposit);
                    }
                    else
                    {
                        _officeSyncCache?.ReplaceDeposit(deposit);
                    }

                    await SyncPaymentDepositIdsForDepositAsync(deposit, currentUser);
                }
                catch (Exception ex)
                {
                    var depositLabel = string.IsNullOrWhiteSpace(deposit.DepositCode)
                        ? deposit.DepositId.ToString()
                        : deposit.DepositCode.Trim();
                    result.Errors.Add($"Deposit {depositLabel} split-link repair: {ex.Message}");
                }

                processed++;
                ReportSyncProgress(progress, "splitLinkRepair", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            foreach (var transfer in transfers)
            {
                result.DocumentsProcessed++;
                try
                {
                    // Pass 1: rematch. Pass 2: after clear/regroup, rematch again (incl. escrow amount packing).
                    for (var pass = 0; pass < 2; pass++)
                    {
                        var originalSplitLineIds = (transfer.Splits ?? [])
                            .Select(split => split.JournalEntryLineId)
                            .ToList();
                        await ReconcileTransferSplitJournalEntryLineIdsAsync(transfer);
                        if (TransferSplitJournalEntryLineIdsChanged(originalSplitLineIds, transfer.Splits))
                        {
                            transfer.ModifiedBy = currentUser;
                            var updated = await _accountingRepository.UpdateTransferAsync(transfer);
                            transfer.Splits = updated.Splits;
                            _officeSyncCache?.ReplaceTransfer(transfer);
                        }
                        else
                        {
                            _officeSyncCache?.ReplaceTransfer(transfer);
                        }

                        if ((await GetUnresolvedTransferSplitMessagesAsync(transfer)).Count == 0)
                            break;
                    }

                    await SyncDepositTransferIdsForTransferAsync(transfer, currentUser);
                }
                catch (Exception ex)
                {
                    var transferLabel = string.IsNullOrWhiteSpace(transfer.TransferCode)
                        ? transfer.TransferId.ToString()
                        : transfer.TransferCode.Trim();
                    result.Errors.Add($"Transfer {transferLabel} split-link repair: {ex.Message}");
                }

                processed++;
                ReportSyncProgress(progress, "splitLinkRepair", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            if (total == 0)
                ReportSyncProgress(progress, "splitLinkRepair", total, processed, result, "Completed");

            return result;
        }, depositsAlreadyLoaded: deposits, transfersAlreadyLoaded: transfers);
    }

    public async Task<JournalEntrySyncResult> SyncPeriodicFeeJournalEntriesAsync(Guid organizationId, string officeIds, DateOnly? startDate = null, DateOnly? endDate = null, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var accountingOffices = (await _organizationRepository.GetAccountingOfficesByOfficeIdsAsync(organizationId, officeIds)).ToList();
        var (clampedStartDate, clampedEndDate) = ClampPeriodicSyncDateRange(startDate, endDate, accountingOffices);

        await SyncDepartureFeesAsync(organizationId, officeIds, clampedStartDate, clampedEndDate, result, progress);
        await ProcessLinenAndTowelFeesAsync(organizationId, officeIds, clampedStartDate, clampedEndDate, result, progress);
        await SyncRetainedEarningsAsync(organizationId, officeIds, clampedStartDate, clampedEndDate, accountingOffices, result, progress);

        return result;
    }

    public async Task<JournalEntrySyncResult> ClearAllJournalEntriesAsync(Guid organizationId, string officeIds)
    {
        var result = new JournalEntrySyncResult();

        try
        {
            // JE line GUIDs on deposits/transfers are about to become invalid — wipe them before deleting JEs.
            await ClearDepositAndTransferSplitJournalEntryLineIdsAsync(organizationId, officeIds);
            result.JournalEntriesDeleted = await _journalEntryRepository.DeleteJournalEntriesByOfficeIdsAsync(organizationId, officeIds);
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task ClearDepositAndTransferSplitJournalEntryLineIdsAsync(Guid organizationId, string officeIds)
    {
        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        foreach (var depositSummary in deposits)
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositSummary.DepositId, organizationId);
            if (deposit?.Splits == null || deposit.Splits.Count == 0)
                continue;

            var changed = false;
            foreach (var split in deposit.Splits)
            {
                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                    continue;

                split.JournalEntryLineId = null;
                changed = true;
            }

            if (changed)
                await _accountingRepository.UpdateDepositAsync(deposit);
        }

        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true
        })).ToList();

        foreach (var transferSummary in transfers)
        {
            var transfer = await _accountingRepository.GetTransferByIdAsync(transferSummary.TransferId, organizationId);
            if (transfer?.Splits == null || transfer.Splits.Count == 0)
                continue;

            var changed = false;
            foreach (var split in transfer.Splits)
            {
                if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
                    continue;

                split.JournalEntryLineId = null;
                changed = true;
            }

            if (changed)
                await _accountingRepository.UpdateTransferAsync(transfer);
        }
    }

    private async Task SyncDepartureFeesAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, JournalEntrySyncResult result, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        ReportSyncProgress(progress, "departureFee", total: 1, processed: 0, result, "Running");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (rangeStart, rangeEnd) = ResolveDepartureFeeDateRange(startDate, endDate, today);
        if (rangeStart > rangeEnd)
        {
            ReportSyncProgress(progress, "departureFee", total: 1, processed: 1, result, "Completed");
            return;
        }

        try
        {
            result.DocumentsProcessed += await ProcessDepartureFeesAsync(organizationId, officeIds, startDate, endDate, CancellationToken.None, logDecisions: true);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Departure fees {rangeStart:yyyy-MM-dd}-{rangeEnd:yyyy-MM-dd}: {ex.Message}");
        }

        ReportSyncProgress(progress, "departureFee", total: 1, processed: 1, result, "Completed");
    }

    private async Task ProcessLinenAndTowelFeesAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, JournalEntrySyncResult result, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        // Sync replays linen/towel once per month in the criteria range, using the last day of each month
        // (e.g. 5/28-7/31 => 5/31, 6/30, 7/31) as the processing date for that month's occupancy check.
        var processingDates = ResolveLinenSyncProcessingDatesInRange(startDate, endDate);
        var total = processingDates.Count;
        var processed = 0;
        ReportSyncProgress(progress, "linenAndTowelFee", total, processed, result, total == 0 ? "Completed" : "Running");

        if (total == 0)
            return;

        var monthlyBatch = (await _propertyRepository.GetMonthlyLinensAndTowelsAsync(organizationId, officeIds)).ToList();
        var annualBatch = (await _propertyRepository.GetAnnualLinensAndTowelsAsync(organizationId, officeIds)).ToList();

        foreach (var runDate in processingDates)
        {
            try
            {
                result.DocumentsProcessed += monthlyBatch.Count + annualBatch.Count;
                await CreateJournalEntriesForLinensAndTowelsAsync(
                    organizationId,
                    monthlyBatch,
                    annualBatch,
                    CancellationToken.None,
                    runDate);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Linen and towel fees {runDate:yyyy-MM-dd}: {ex.Message}");
            }

            processed++;
            ReportSyncProgress(progress, "linenAndTowelFee", total, processed, result, processed >= total ? "Completed" : "Running");
        }
    }

    private async Task SyncBillPaymentJournalEntryAsync(Receipt bill, Guid currentUser, JournalEntrySyncResult result)
    {
        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(bill.OrganizationId, bill.OfficeId);
        var paymentApplication = new BillPaymentApplication
        {
            Bill = bill,
            AmountApplied = bill.PaidAmount,
            PaymentDate = bill.PaidDate ?? bill.ReceiptDate,
            ChartOfAccountId = GetDefaultBankAccount(chartOfAccounts, bill.OfficeId, accountingOffice),
            Description = bill.PaymentDescription?.Trim() ?? string.Empty,
            PaymentSequence = 0
        };

        await TrackJournalEntryCreateAsync(
            () => CreateJournalEntryFromBillPaymentWithResultAsync(paymentApplication, currentUser),
            new JournalEntryGetCriteria
            {
                OrganizationId = bill.OrganizationId,
                OfficeIds = bill.OfficeId.ToString(),
                SourceTypeId = (int)SourceType.BillPayment,
                SourceId = bill.ReceiptId,
                IncludeUnposted = true
            },
            result);
    }

    private async Task<JournalEntrySyncResult> ClearJournalEntriesBySourceTypesAsync(Guid organizationId, string officeIds, params int[] sourceTypeIds)
    {
        var result = new JournalEntrySyncResult();

        foreach (var sourceTypeId in sourceTypeIds)
        {
            var entries = (await _journalEntryRepository.GetJournalEntriesAsync(new JournalEntryGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                SourceTypeId = sourceTypeId,
                // Clear must delete JEs before the accounting-office start date.
                StartDate = DateOnly.MinValue,
                IncludeUnposted = true
            })).ToList();

            foreach (var entry in entries)
            {
                try
                {
                    await DeleteOpenJournalEntryAsync(entry.JournalEntryId, organizationId);
                    result.JournalEntriesDeleted++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Journal entry {entry.JournalEntryCode}: {ex.Message}");
                }
            }
        }

        await _organizationManager.ResetEntityCodeSequenceAsync(organizationId, EntityType.JournalEntry, 0);

        return result;
    }

    private async Task TrackJournalEntryCreateAsync(Func<Task<AccountingJournalEntryResult>> createJournalEntry, JournalEntryGetCriteria existingCriteria, JournalEntrySyncResult result)
    {
        // Skip check must see JEs before the accounting-office start date; otherwise Sync recreates duplicates.
        existingCriteria.StartDate = DateOnly.MinValue;

        var existingEntries = await GetJournalEntriesByCriteriaCachedAsync(existingCriteria);
        if (existingEntries.Any())
        {
            result.JournalEntriesSkipped++;
            return;
        }

        var createResult = await createJournalEntry();
        if (createResult.JournalEntry != null)
            result.JournalEntriesCreated++;

        if (createResult.HasWarning)
            result.Errors.Add(createResult.Warning!);
    }

    private static void ReportSyncProgress(IProgress<JournalEntrySyncProgress>? progress, string syncType, int total, int processed, JournalEntrySyncResult result, string status)
    {
        progress?.Report(new JournalEntrySyncProgress
        {
            SyncType = syncType,
            Total = total,
            Processed = processed,
            Skipped = result.JournalEntriesSkipped,
            Errors = result.Errors.Count,
            Status = status
        });
    }

    private async Task SyncRetainedEarningsAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, IReadOnlyCollection<AccountingOffice> accountingOffices, JournalEntrySyncResult result, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var (jeStartDate, jeEndDate) = await ResolveRetainedEarningsSyncDateRangeFromJournalEntriesAsync(organizationId, officeIds);
        var syncStartDate = startDate ?? jeStartDate;
        var syncEndDate = endDate ?? jeEndDate;
        (syncStartDate, syncEndDate) = ClampPeriodicSyncDateRange(syncStartDate, syncEndDate, accountingOffices);
        var processingDates = ResolveRetainedEarningsSyncProcessingDatesInRange(accountingOffices, syncStartDate, syncEndDate);
        ReportSyncProgress(progress, "retainedEarnings", total: 1, processed: 0, result, processingDates.Count == 0 ? "Completed" : "Running");

        if (processingDates.Count == 0)
            return;

        var rangeStart = syncStartDate ?? syncEndDate!.Value;
        var rangeEnd = syncEndDate ?? syncStartDate!.Value;
        if (rangeStart > rangeEnd)
            (rangeStart, rangeEnd) = (rangeEnd, rangeStart);

        try
        {
            result.DocumentsProcessed += await ProcessRetainedEarningsAsync(organizationId, officeIds, syncStartDate, syncEndDate, CancellationToken.None, logDecisions: true);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Retained earnings {rangeStart:yyyy-MM-dd}-{rangeEnd:yyyy-MM-dd}: {ex.Message}");
        }

        ReportSyncProgress(progress, "retainedEarnings", total: 1, processed: 1, result, "Completed");
    }

    private static List<DateOnly> ResolveLinenSyncProcessingDatesInRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return [];

        var rangeStart = startDate ?? endDate!.Value;
        var rangeEnd = endDate ?? startDate!.Value;
        if (rangeStart > rangeEnd)
            (rangeStart, rangeEnd) = (rangeEnd, rangeStart);

        var dates = new List<DateOnly>();
        var monthCursor = new DateOnly(rangeStart.Year, rangeStart.Month, 1);
        var lastMonthStart = new DateOnly(rangeEnd.Year, rangeEnd.Month, 1);
        while (monthCursor <= lastMonthStart)
        {
            dates.Add(monthCursor.AddMonths(1).AddDays(-1));
            monthCursor = monthCursor.AddMonths(1);
        }

        return dates;
    }
}
