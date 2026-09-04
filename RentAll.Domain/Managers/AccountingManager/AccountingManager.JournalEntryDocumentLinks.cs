using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task SyncDocumentLinksAsync(
        Guid organizationId,
        string officeIds,
        Guid currentUser,
        IProgress<JournalEntrySyncProgress>? progress = null)
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
                        && string.Equals(journalEntry.PaymentCode, payment.PaymentCode, StringComparison.Ordinal))
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
}
