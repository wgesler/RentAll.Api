using RentAll.Domain.Enums;
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

        var linkResult = new JournalEntrySyncResult();
        var payments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds))
            .Where(payment => payment.IsActive)
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.PaymentId)
            .ToList();

        var processed = 0;
        var total = payments.Count;
        ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, total == 0 ? "Completed" : "Running");

        foreach (var paymentSummary in payments)
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
            if (payment != null && payment.IsActive)
                await SyncPaymentDocumentLinksAsync(payment, currentUser);

            processed++;
            ReportSyncProgress(progress, "documentLinkPayment", total, processed, linkResult, processed >= total ? "Completed" : "Running");
        }

        await SyncInvoicePaymentDocumentLinksFromLedgerAsync(organizationId, officeIds, currentUser);

        var deposits = (await _accountingRepository.GetDepositsByCriteriaAsync(new DepositGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        processed = 0;
        total = deposits.Count;
        ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, total == 0 ? "Completed" : "Running");

        foreach (var depositSummary in deposits)
        {
            var deposit = await _accountingRepository.GetDepositByIdAsync(depositSummary.DepositId, organizationId);
            if (deposit != null && deposit.IsActive)
                await SyncDepositDocumentLinksAsync(deposit, currentUser);

            processed++;
            ReportSyncProgress(progress, "documentLinkDeposit", total, processed, linkResult, processed >= total ? "Completed" : "Running");
        }

        var transfers = (await _accountingRepository.GetTransfersByCriteriaAsync(new TransferGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false
        })).ToList();

        processed = 0;
        total = transfers.Count;
        ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, total == 0 ? "Completed" : "Running");

        foreach (var transferSummary in transfers)
        {
            var transfer = await _accountingRepository.GetTransferByIdAsync(transferSummary.TransferId, organizationId);
            if (transfer != null && transfer.IsActive)
                await SyncTransferDocumentLinksAsync(transfer, currentUser);

            processed++;
            ReportSyncProgress(progress, "documentLinkTransfer", total, processed, linkResult, processed >= total ? "Completed" : "Running");
        }

        // After JE rebuild + document links, rematch deposit UF lines and transfer escrow lines
        // so transfer reports do not fail on stale JournalEntryLineId values.
        await RepairDepositAndTransferSplitLinksAsync(organizationId, officeIds, currentUser, progress);
    }

    private async Task SyncInvoicePaymentDocumentLinksFromLedgerAsync(Guid organizationId, string officeIds, Guid currentUser)
    {
        var invoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            IncludePaid = true
        })).ToList();

        foreach (var invoiceSummary in invoices)
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId);
            if (invoice == null)
                continue;

            var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
            foreach (var ledgerLine in invoice.LedgerLines.Where(line => line.Amount != 0))
            {
                if (!costCodeById.TryGetValue(ledgerLine.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                    continue;

                if (ledgerLine.PaymentId is not { } paymentId || paymentId == Guid.Empty)
                    continue;

                var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
                if (payment == null || !payment.IsActive)
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
