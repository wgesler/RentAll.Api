using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Health Document Links
    public async Task SyncDocumentLinksAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (string.IsNullOrWhiteSpace(officeIds))
            throw new ArgumentException("OfficeIds is required.", nameof(officeIds));

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return;

        await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var linkResult = new JournalEntrySyncResult();
            var payments = _officeSyncCache!.Payments
                .OrderBy(payment => payment.PaymentDate)
                .ThenBy(payment => payment.PaymentId)
                .ToList();

            var processed = 0;
            var total = payments.Count;
            ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var payment in payments)
            {
                await SyncPaymentDocumentLinksAsync(payment, currentUser);

                processed++;
                ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }

            await SyncInvoicePaymentDocumentLinksFromLedgerAsync(organizationId, officeIds, currentUser);

            var deposits = _officeSyncCache.Deposits
                .Where(deposit => deposit.IsActive)
                .ToList();

            processed = 0;
            total = deposits.Count;
            ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var deposit in deposits)
            {
                await SyncDepositDocumentLinksAsync(deposit, currentUser);

                processed++;
                ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }

            var transfers = _officeSyncCache.Transfers
                .Where(transfer => transfer.IsActive)
                .ToList();

            processed = 0;
            total = transfers.Count;
            ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var transfer in transfers)
            {
                await SyncTransferDocumentLinksAsync(transfer, currentUser);

                processed++;
                ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }

            // After JE rebuild + document links, rematch deposit UF lines and transfer escrow lines
            // so transfer reports do not fail on stale JournalEntryLineId values.
            await RepairDepositAndTransferSplitLinksAsync(organizationId, officeIds, currentUser, progress);
        });
    }

    public async Task<JournalEntrySyncResult> RepairDocumentLinksForHealthFixAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));

        if (string.IsNullOrWhiteSpace(officeIds))
            throw new ArgumentException("OfficeIds is required.", nameof(officeIds));

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return new JournalEntrySyncResult();

        var scan = await _healthRepository.RunDocumentLinksHealthCheckAsync(organizationId, officeIds);
        var result = new JournalEntrySyncResult();

        if (scan.Summary.IsClean)
            return result;

        var paymentIds = new HashSet<Guid>();
        var depositIds = new HashSet<Guid>();
        var transferIds = new HashSet<Guid>();
        HealthDocumentTypeIdentification.CollectFixTargets(scan.Issues, paymentIds, depositIds, transferIds);
        await CollectDepositedPaymentDepositIdsForDocumentLinkFixAsync(organizationId, scan.Issues, paymentIds, depositIds);
        CollectDepositIdsFromUfSplitDocumentLinkIssues(scan.Issues, depositIds);

        // Link fix realigns JE/document stamps from current splits — do not run deposit/payment/transfer rematch here (that belongs on those Fix tabs).
        await ReconcileDuplicateInvoicePaymentDocumentsForIssuesAsync(scan.Issues, organizationId, currentUser, result);

        await SyncDocumentLinksForHealthFixTargetsAsync(
            organizationId,
            officeIds,
            paymentIds,
            depositIds,
            transferIds,
            currentUser,
            progress);

        return result;
    }

    private static void CollectDepositIdsFromUfSplitDocumentLinkIssues(
        IReadOnlyList<DocumentHealthIssue>? issues,
        ISet<Guid> depositIds)
    {
        foreach (var issue in issues ?? [])
        {
            if (!(issue.Issue ?? string.Empty).Contains("Deposit UF split", StringComparison.OrdinalIgnoreCase))
                continue;

            if (issue.DocumentId != Guid.Empty)
                depositIds.Add(issue.DocumentId);
        }
    }

    private async Task SyncDocumentLinksForHealthFixTargetsAsync(Guid organizationId, string officeIds, IReadOnlySet<Guid> paymentIds, IReadOnlySet<Guid> depositIds, IReadOnlySet<Guid> transferIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress)
    {
        if (paymentIds.Count == 0 && depositIds.Count == 0 && transferIds.Count == 0)
            return;

        await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var linkResult = new JournalEntrySyncResult();
            var payments = _officeSyncCache!.Payments
                .Where(payment => paymentIds.Contains(payment.PaymentId))
                .OrderBy(payment => payment.PaymentDate)
                .ThenBy(payment => payment.PaymentId)
                .ToList();

            var processed = 0;
            var total = payments.Count;
            ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var payment in payments)
            {
                await SyncPaymentDocumentLinksAsync(payment, currentUser);
                processed++;
                ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }

            var deposits = _officeSyncCache.Deposits
                .Where(deposit => deposit.IsActive && depositIds.Contains(deposit.DepositId))
                .ToList();

            processed = 0;
            total = deposits.Count;
            ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var deposit in deposits)
            {
                var reloaded = await _accountingRepository.GetDepositByIdAsync(deposit.DepositId, organizationId) ?? deposit;
                await SyncDepositDocumentLinksAsync(reloaded, currentUser, unstampMissingPayments: false);
                processed++;
                ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }

            var transfers = _officeSyncCache.Transfers
                .Where(transfer => transfer.IsActive && transferIds.Contains(transfer.TransferId))
                .ToList();

            processed = 0;
            total = transfers.Count;
            ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, total == 0 ? "Completed" : "Running");

            foreach (var transfer in transfers)
            {
                await SyncTransferDocumentLinksAsync(transfer, currentUser);
                processed++;
                ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, processed >= total ? "Completed" : "Running");
            }
        });
    }

    private async Task CollectDepositedPaymentDepositIdsForDocumentLinkFixAsync(
        Guid organizationId,
        IReadOnlyList<DocumentHealthIssue>? issues,
        ISet<Guid> paymentIds,
        ISet<Guid> depositIds)
    {
        foreach (var issue in issues ?? [])
        {
            if (!(issue.Issue ?? string.Empty).Contains(
                    "Deposited payment journal entry missing deposit stamp",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (issue.DocumentId == Guid.Empty)
                continue;

            paymentIds.Add(issue.DocumentId);

            Payment? payment = null;
            if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(issue.DocumentId, out var cachedPayment))
                payment = cachedPayment;
            else
                payment = await _accountingRepository.GetPaymentByIdAsync(issue.DocumentId, organizationId);

            if (payment?.DepositId is { } depositId && depositId != Guid.Empty)
                depositIds.Add(depositId);
        }
    }

    private static void MergeSyncResults(JournalEntrySyncResult target, JournalEntrySyncResult source)
    {
        target.DocumentsProcessed += source.DocumentsProcessed;
        target.JournalEntriesCreated += source.JournalEntriesCreated;
        target.JournalEntriesSkipped += source.JournalEntriesSkipped;
        target.JournalEntriesDeleted += source.JournalEntriesDeleted;
        target.Errors.AddRange(source.Errors);
    }

    private async Task SyncInvoicePaymentDocumentLinksFromLedgerAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        var invoices = _officeSyncCache != null
            ? _officeSyncCache.InvoicesById.Values.ToList()
            : (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                IncludeInactive = true,
                IncludePaid = true
            })).ToList();

        foreach (var invoiceSummary in invoices)
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId)
                ?? invoiceSummary;

            var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
            foreach (var ledgerLine in invoice.LedgerLines.Where(line => line.Amount != 0))
            {
                if (!costCodeById.TryGetValue(ledgerLine.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                    continue;

                if (ledgerLine.PaymentId is not { } paymentId || paymentId == Guid.Empty)
                    continue;

                Payment? payment = null;
                if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
                    payment = cachedPayment;
                else
                    payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);

                if (payment == null)
                    continue;

                await EnsurePaymentCodePersistedAsync(payment, currentUser);

                var paymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                    invoice.OrganizationId,
                    invoice.OfficeId,
                    invoice,
                    ledgerLine);

                foreach (var journalEntry in paymentEntries)
                {
                    if (journalEntry.PaymentId == payment.PaymentId
                        && EntityCodeFormatting.CodesMatch(journalEntry.PaymentCode, payment.PaymentCode))
                    {
                        continue;
                    }

                    ApplyPaymentDocumentLink(journalEntry, payment);
                    journalEntry.ModifiedBy = currentUser;
                    await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
                }
            }
        }
    }

    private async Task EnsurePaymentCodePersistedAsync(Payment payment, Guid currentUser)
    {
        var originalCode = payment.PaymentCode;
        await EnsurePaymentCodeAsync(payment);
        if (string.Equals(originalCode, payment.PaymentCode, StringComparison.Ordinal))
            return;

        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
    }
    #endregion
}
