using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Journal Entry
    public async Task<List<JournalEntry>> CreateJournalEntriesFromPaymentDocumentAsync(Guid paymentId, Guid organizationId, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

        if (paymentId == Guid.Empty)
            return journalEntries;

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return journalEntries;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || payment.LedgerLines.Count == 0)
            return journalEntries;

        var applications = await LoadPaymentApplicationsAsync(payment, organizationId);
        if (applications.Count == 0)
            return journalEntries;

        ValidatePaymentApplicationsTotal(payment, applications);

        var existingPaymentDocumentEntries = await GetJournalEntriesForSourceAsync(
            payment.OrganizationId,
            payment.OfficeId,
            SourceType.InvoicePayment,
            payment.PaymentId);

        foreach (var consolidatedEntry in existingPaymentDocumentEntries)
            await DeleteOpenJournalEntryAsync(consolidatedEntry.JournalEntryId, payment.OrganizationId);

        foreach (var application in applications)
        {
            var existingPaymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                application.Invoice.OrganizationId,
                application.Invoice.OfficeId,
                application.Invoice,
                application.PaymentLedgerLine);
            var paymentResult = await UpsertInvoicePaymentSideEffectsAsync(
                application.Invoice,
                application.PaymentLedgerLine,
                existingPaymentEntries,
                currentUser,
                createMainCashJournalEntry: true);
            if (paymentResult.JournalEntry != null
                && !journalEntries.Any(entry => entry.JournalEntryId == paymentResult.JournalEntry.JournalEntryId))
            {
                journalEntries.Add(paymentResult.JournalEntry);
            }
        }

        await SyncPaymentDocumentLinksAsync(payment, currentUser);

        return journalEntries;
    }

    public async Task<JournalEntrySyncResult> SyncPaymentJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null)
    {
        var result = new JournalEntrySyncResult();
        var payments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds))
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.PaymentId)
            .ToList();
        var total = payments.Count;
        var processed = 0;
        ReportSyncProgress(progress, "payment", total, processed, result, "Running");

        foreach (var paymentSummary in payments)
        {
            result.DocumentsProcessed++;

            try
            {
                var createdEntries = await CreateJournalEntriesFromPaymentDocumentAsync(
                    paymentSummary.PaymentId,
                    organizationId,
                    currentUser);
                if (createdEntries.Count > 0 || await PaymentHasJournalEntriesAsync(paymentSummary.PaymentId, organizationId))
                    result.JournalEntriesCreated++;
                else
                    result.JournalEntriesSkipped++;
            }
            catch (Exception ex)
            {
                var message = $"Payment {paymentSummary.Description}: {ex.Message}";
                result.Errors.Add(message);
                await LogAccountingErrorAsync(
                    trigger: "Payment",
                    organizationId: organizationId,
                    officeId: paymentSummary.OfficeId,
                    sourceTypeId: (int)SourceType.InvoicePayment,
                    sourceId: paymentSummary.PaymentId,
                    documentCode: paymentSummary.Description,
                    accountingPeriod: null,
                    amount: paymentSummary.Amount,
                    message: message,
                    currentUser: currentUser);
            }

            processed++;
            ReportSyncProgress(progress, "payment", total, processed, result, processed >= total ? "Completed" : "Running");
        }

        foreach (var invoiceSummary in (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            IncludePaid = true
        })).ToList())
        {
            try
            {
                var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId);
                if (invoice == null)
                    continue;

                var costCodesByOffice = await LoadCostCodeByOfficeIdAsync(organizationId, invoice.OfficeId);
                foreach (var line in invoice.LedgerLines.Where(line => line.Amount != 0))
                {
                    if (!costCodesByOffice.TryGetValue(line.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                        continue;

                    if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                        continue;

                    var existingPaymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                        invoice.OrganizationId,
                        invoice.OfficeId,
                        invoice,
                        line);
                    await UpsertJournalEntryFromPaymentAsync(invoice, line, existingPaymentEntries, currentUser);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Invoice {invoiceSummary.InvoiceCode} legacy payment: {ex.Message}");
            }
        }

        if (total == 0)
            ReportSyncProgress(progress, "payment", total, processed, result, "Completed");

        await SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress);

        return result;
    }
    #endregion

    #region Helpers
    private async Task<List<PaymentApplicationContext>> LoadPaymentApplicationsAsync(Payment payment, Guid organizationId)
    {
        var applications = new List<PaymentApplicationContext>();

        foreach (var paymentLine in payment.LedgerLines.OrderBy(line => line.InvoiceCode).ThenBy(line => line.LineNumber))
        {
            if (paymentLine.LedgerLineId == Guid.Empty || paymentLine.Amount == 0)
                continue;

            var invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            if (invoice == null)
                throw new Exception($"Invoice not found for payment allocation: {paymentLine.InvoiceCode}");

            var paymentLedgerLine = invoice.LedgerLines.SingleOrDefault(line => line.LedgerLineId == paymentLine.LedgerLineId);
            if (paymentLedgerLine == null)
                throw new Exception($"Payment ledger line not found on invoice {invoice.InvoiceCode}");

            paymentLedgerLine.PaymentId = payment.PaymentId;
            applications.Add(new PaymentApplicationContext(invoice, paymentLedgerLine));
        }

        return applications;
    }

    private static void ValidatePaymentApplicationsTotal(Payment payment, IReadOnlyList<PaymentApplicationContext> applications)
    {
        var allocationTotal = applications.Sum(application => application.PaymentLedgerLine.Amount);
        if (allocationTotal != payment.Amount)
            throw new Exception($"Payment allocation total {allocationTotal:0.00} does not match payment amount {payment.Amount:0.00}.");
    }

    private sealed record PaymentApplicationContext(Invoice Invoice, LedgerLine PaymentLedgerLine);

    private async Task<bool> PaymentHasJournalEntriesAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return false;

        var linkedEntries = await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria {OrganizationId = organizationId, PaymentId = paymentId});
        if (linkedEntries.Any())
            return true;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            return false;

        foreach (var paymentLine in payment.LedgerLines.Where(line => line.Amount != 0))
        {
            if (paymentLine.InvoiceId == Guid.Empty)
                continue;

            var invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            if (invoice == null)
                continue;

            var paymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                invoice,
                ToInvoicePaymentLedgerLine(paymentLine));
            if (paymentEntries.Any())
                return true;
        }

        return false;
    }
    #endregion

    #region Document Link
    private async Task ApplyPaymentDocumentLinkAsync(JournalEntry journalEntry, LedgerLine paymentLedgerLine, Guid organizationId)
    {
        if (paymentLedgerLine.PaymentId is not { } paymentId || paymentId == Guid.Empty)
            return;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId)
            ?? throw new Exception($"Payment record not found for PaymentId {paymentId}.");

        ApplyPaymentDocumentLink(journalEntry, payment);
    }

    private static void ApplyPaymentDocumentLink(JournalEntry journalEntry, Payment payment)
    {
        if (payment.PaymentId == Guid.Empty)
            return;

        journalEntry.PaymentId = payment.PaymentId;
        journalEntry.PaymentCode = payment.PaymentCode.Trim();
    }

    private static void ClearPaymentDocumentLink(JournalEntry journalEntry)
    {
        journalEntry.PaymentId = null;
        journalEntry.PaymentCode = null;
    }

    private async Task SyncPaymentDocumentLinksAsync(Payment payment, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty || !payment.IsActive)
            return;

        if (!await IsAccountingFeatureEnabledAsync(payment.OrganizationId))
            return;

        await EnsurePaymentCodePersistedAsync(payment, currentUser);

        await ClearPaymentDocumentLinksAsync(payment.OrganizationId, payment.PaymentId, currentUser);

        var linkedEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
        {
            OrganizationId = payment.OrganizationId,
            PaymentId = payment.PaymentId
        })).ToList();

        foreach (var journalEntry in linkedEntries)
        {
            ApplyPaymentDocumentLink(journalEntry, payment);
            journalEntry.ModifiedBy = currentUser;
            await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
        }

        foreach (var paymentLine in payment.LedgerLines.Where(line => line.Amount != 0))
        {
            if (paymentLine.InvoiceId == Guid.Empty)
                continue;

            var invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, payment.OrganizationId);
            if (invoice == null)
                continue;

            var ledgerLine = ToInvoicePaymentLedgerLine(paymentLine);
            var paymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                invoice.OrganizationId,
                invoice.OfficeId,
                invoice,
                ledgerLine);

            foreach (var journalEntry in paymentEntries)
            {
                ApplyPaymentDocumentLink(journalEntry, payment);
                journalEntry.ModifiedBy = currentUser;
                await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
            }
        }
    }

    private async Task ClearPaymentDocumentLinksAsync(Guid organizationId, Guid paymentId, Guid currentUser)
    {
        if (paymentId == Guid.Empty)
            return;

        var linkedEntries = (await _journalEntryRepository.GetJournalEntriesByPaymentIdAsync(new JournalEntryGetByPaymentIdCriteria
        {
            OrganizationId = organizationId,
            PaymentId = paymentId
        })).ToList();

        foreach (var journalEntry in linkedEntries)
        {
            ClearPaymentDocumentLink(journalEntry);
            journalEntry.ModifiedBy = currentUser;
            await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
        }
    }
    #endregion
}
