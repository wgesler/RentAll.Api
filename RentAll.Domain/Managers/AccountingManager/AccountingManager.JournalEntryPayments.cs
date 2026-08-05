using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Text;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Journal Entry
    public async Task<List<JournalEntry>> CreateJournalEntriesFromPaymentDocumentAsync(Guid paymentId, Guid organizationId, Guid currentUser, bool allowPartialAllocationsOnMismatch = false)
    {
        var journalEntries = new List<JournalEntry>();

        if (paymentId == Guid.Empty)
            return journalEntries;

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return journalEntries;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || payment.LedgerLines.Count == 0)
            return journalEntries;

        var loadResult = await LoadPaymentApplicationsAsync(payment, organizationId, strict: !allowPartialAllocationsOnMismatch);
        if (loadResult.Applications.Count == 0)
            return journalEntries;

        var allocationTotal = loadResult.Applications.Sum(application => application.PaymentLedgerLine.Amount);
        if (allocationTotal != payment.Amount)
        {
            var diagnostic = BuildPaymentAllocationDiagnosticMessage(payment, loadResult, allocationTotal);
            if (!allowPartialAllocationsOnMismatch)
                throw new Exception(diagnostic);

            await LogAccountingErrorAsync(
                trigger: "Payment",
                organizationId: payment.OrganizationId,
                officeId: payment.OfficeId,
                sourceTypeId: (int)SourceType.InvoicePayment,
                sourceId: payment.PaymentId,
                documentCode: payment.PaymentCode,
                accountingPeriod: null,
                amount: payment.Amount,
                message: diagnostic,
                currentUser: currentUser);
        }

        return await CreateJournalEntriesFromPaymentApplicationsAsync(payment, loadResult.Applications, currentUser);
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
                var createdEntries = await CreateJournalEntriesFromPaymentDocumentAsync(paymentSummary.PaymentId, organizationId, currentUser, allowPartialAllocationsOnMismatch: true);
                if (createdEntries.Count > 0 || await PaymentHasJournalEntriesAsync(paymentSummary.PaymentId, organizationId))
                {
                    result.JournalEntriesCreated++;
                }
                else
                {
                    result.JournalEntriesSkipped++;
                    var payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
                    var skipMessage = await BuildPaymentSkipDiagnosticMessageAsync(payment, paymentSummary, organizationId);
                    await LogAccountingErrorAsync(
                        trigger: "PaymentSkip",
                        organizationId: organizationId,
                        officeId: paymentSummary.OfficeId,
                        sourceTypeId: (int)SourceType.InvoicePayment,
                        sourceId: paymentSummary.PaymentId,
                        documentCode: ResolvePaymentDocumentCode(payment, paymentSummary),
                        accountingPeriod: null,
                        amount: paymentSummary.Amount,
                        message: skipMessage,
                        currentUser: currentUser);
                }
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

        await CreateJournalEntriesFromUnlinkedInvoicePaymentLinesAsync(organizationId, officeIds, currentUser, result);

        if (total == 0)
            ReportSyncProgress(progress, "payment", total, processed, result, "Completed");

        await SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress);

        return result;
    }
    #endregion

    #region Helpers
    private async Task<List<JournalEntry>> CreateJournalEntriesFromPaymentApplicationsAsync(Payment payment, IReadOnlyList<PaymentApplicationContext> applications, Guid currentUser)
    {
        var journalEntries = new List<JournalEntry>();

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

    private async Task CreateJournalEntriesFromUnlinkedInvoicePaymentLinesAsync(Guid organizationId, string officeIds, Guid currentUser, JournalEntrySyncResult result)
    {
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
    }

    private async Task<PaymentApplicationLoadResult> LoadPaymentApplicationsAsync(Payment payment, Guid organizationId, bool strict)
    {
        var applications = new List<PaymentApplicationContext>();
        var skippedLines = new List<PaymentLedgerLineLoadIssue>();
        var failedLines = new List<PaymentLedgerLineLoadIssue>();

        foreach (var paymentLine in payment.LedgerLines.OrderBy(line => line.InvoiceCode).ThenBy(line => line.LineNumber))
        {
            if (paymentLine.LedgerLineId == Guid.Empty)
            {
                skippedLines.Add(new PaymentLedgerLineLoadIssue(paymentLine, "LedgerLineId is empty"));
                continue;
            }

            if (paymentLine.Amount == 0)
            {
                skippedLines.Add(new PaymentLedgerLineLoadIssue(paymentLine, "Amount is zero"));
                continue;
            }

            var invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            if (invoice == null)
            {
                var reason = $"Invoice not found: {paymentLine.InvoiceCode}";
                if (strict)
                    throw new Exception(reason);

                failedLines.Add(new PaymentLedgerLineLoadIssue(paymentLine, reason));
                continue;
            }

            var paymentLedgerLine = invoice.LedgerLines.SingleOrDefault(line => line.LedgerLineId == paymentLine.LedgerLineId);
            if (paymentLedgerLine == null)
            {
                var reason = $"Payment ledger line not found on invoice {invoice.InvoiceCode} (line may have been removed from the invoice without deleting the payment).";
                if (strict)
                    throw new Exception(reason);

                failedLines.Add(new PaymentLedgerLineLoadIssue(paymentLine, reason));
                continue;
            }

            paymentLedgerLine.PaymentId = payment.PaymentId;
            applications.Add(new PaymentApplicationContext(invoice, paymentLedgerLine));
        }

        return new PaymentApplicationLoadResult(applications, skippedLines, failedLines);
    }

    private static string BuildPaymentAllocationDiagnosticMessage(Payment payment, PaymentApplicationLoadResult loadResult, decimal resolvedApplicationTotal)
    {
        var linkedLedgerLineTotal = payment.LedgerLines.Sum(line => line.Amount);
        var builder = new StringBuilder();
        builder.AppendLine($"Payment allocation mismatch for {payment.Description.Trim()} ({payment.PaymentCode}).");
        builder.AppendLine($"PaymentId: {payment.PaymentId}");
        builder.AppendLine($"PaymentDate: {payment.PaymentDate:yyyy-MM-dd}");
        builder.AppendLine($"Header amount: {payment.Amount:0.00}");
        builder.AppendLine($"Linked ledger lines: {payment.LedgerLines.Count} totaling {linkedLedgerLineTotal:0.00}");
        builder.AppendLine($"Resolved applications: {loadResult.Applications.Count} totaling {resolvedApplicationTotal:0.00}");
        builder.AppendLine($"Unallocated on payment header: {(payment.Amount - resolvedApplicationTotal):0.00}");

        if (loadResult.Applications.Count > 0)
        {
            builder.AppendLine("Applied lines:");
            foreach (var application in loadResult.Applications)
            {
                var line = application.PaymentLedgerLine;
                builder.AppendLine(
                    $"  - {application.Invoice.InvoiceCode} line {line.LineNumber}: {line.Amount:0.00} ({line.Description}) [LedgerLineId={line.LedgerLineId}]");
            }
        }

        AppendPaymentLedgerLineIssues(builder, "Skipped linked lines", loadResult.SkippedLines);
        AppendPaymentLedgerLineIssues(builder, "Failed linked lines", loadResult.FailedLines);

        builder.Append(
            "Likely cause: invoice payment lines were removed or changed without updating the Payment header (use Delete Payment or Update Payment with allocations to keep them in sync).");

        return builder.ToString().Trim();
    }

    private async Task<string> BuildPaymentSkipDiagnosticMessageAsync(Payment? payment, Payment paymentSummary, Guid organizationId)
    {
        var builder = new StringBuilder();
        AppendPaymentIdentity(builder, payment, paymentSummary);
        builder.AppendLine($"Description: {ResolvePaymentDescription(payment, paymentSummary)}");

        if (payment == null)
        {
            builder.Append("Reason: payment record not found.");
            return builder.ToString().Trim();
        }

        builder.AppendLine($"PaymentDate: {payment.PaymentDate:yyyy-MM-dd}");
        builder.AppendLine($"Header amount: {payment.Amount:0.00}");

        if (payment.LedgerLines.Count == 0)
        {
            builder.Append("Reason: no linked invoice ledger lines.");
            return builder.ToString().Trim();
        }

        var linkedLedgerLineTotal = payment.LedgerLines.Sum(line => line.Amount);
        builder.AppendLine($"Linked ledger lines: {payment.LedgerLines.Count} totaling {linkedLedgerLineTotal:0.00}");

        var loadResult = await LoadPaymentApplicationsAsync(payment, organizationId, strict: false);
        if (loadResult.Applications.Count == 0)
        {
            builder.AppendLine("Reason: linked ledger lines exist but none resolved to invoice payment lines.");
            AppendPaymentLedgerLineIssues(builder, "Skipped linked lines", loadResult.SkippedLines);
            AppendPaymentLedgerLineIssues(builder, "Failed linked lines", loadResult.FailedLines);
            return builder.ToString().Trim();
        }

        var applicationTotal = loadResult.Applications.Sum(application => application.PaymentLedgerLine.Amount);
        builder.AppendLine($"Resolved applications: {loadResult.Applications.Count} totaling {applicationTotal:0.00}");
        builder.Append("Reason: payment applications were processed but no journal entries were created or found.");
        return builder.ToString().Trim();
    }

    private static void AppendPaymentIdentity(StringBuilder builder, Payment? payment, Payment paymentSummary)
    {
        builder.AppendLine($"PaymentId: {paymentSummary.PaymentId}");
        builder.AppendLine($"PaymentCode: {ResolvePaymentDocumentCode(payment, paymentSummary)}");
    }

    private static string ResolvePaymentDocumentCode(Payment? payment, Payment paymentSummary)
    {
        if (!string.IsNullOrWhiteSpace(payment?.PaymentCode))
            return payment.PaymentCode.Trim();

        if (!string.IsNullOrWhiteSpace(paymentSummary.PaymentCode))
            return paymentSummary.PaymentCode.Trim();

        return paymentSummary.PaymentId.ToString();
    }

    private static string ResolvePaymentDescription(Payment? payment, Payment paymentSummary)
    {
        if (!string.IsNullOrWhiteSpace(payment?.Description))
            return payment.Description.Trim();

        if (!string.IsNullOrWhiteSpace(paymentSummary.Description))
            return paymentSummary.Description.Trim();

        return ResolvePaymentDocumentCode(payment, paymentSummary);
    }

    private static void AppendPaymentLedgerLineIssues(StringBuilder builder, string heading, IReadOnlyList<PaymentLedgerLineLoadIssue> issues)
    {
        if (issues.Count == 0)
            return;

        builder.AppendLine($"{heading}:");
        foreach (var issue in issues)
        {
            builder.AppendLine(
                $"  - {issue.Line.InvoiceCode} line {issue.Line.LineNumber}: {issue.Line.Amount:0.00} ({issue.Line.Description}) [LedgerLineId={issue.Line.LedgerLineId}] — {issue.Reason}");
        }
    }

    private sealed record PaymentApplicationContext(Invoice Invoice, LedgerLine PaymentLedgerLine);

    private sealed record PaymentLedgerLineLoadIssue(PaymentLedgerLine Line, string Reason);

    private sealed record PaymentApplicationLoadResult(IReadOnlyList<PaymentApplicationContext> Applications, IReadOnlyList<PaymentLedgerLineLoadIssue> SkippedLines, IReadOnlyList<PaymentLedgerLineLoadIssue> FailedLines);

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
