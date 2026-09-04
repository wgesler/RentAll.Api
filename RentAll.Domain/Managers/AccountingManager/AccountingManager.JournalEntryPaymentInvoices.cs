using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Text;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Journal Entry
    public async Task<List<JournalEntry>> CreateJournalEntriesFromInvoicePaymentDocumentAsync(Guid paymentId, Guid organizationId, Guid currentUser, bool allowPartialAllocationsOnMismatch = false)
    {
        var result = await CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(
            paymentId,
            organizationId,
            currentUser,
            allowPartialAllocationsOnMismatch);

        if (result.JournalEntries.Count == 0 && paymentId != Guid.Empty)
        {
            var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
            if (payment?.LedgerLines.Count > 0)
            {
                throw new Exception(
                    $"Payment journal entry sync failed.{Environment.NewLine}{result.FormatBailTrail()}");
            }
        }

        return result.JournalEntries;
    }

    private async Task<PaymentJournalEntryCreateResult> CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(Guid paymentId, Guid organizationId, Guid currentUser, bool allowPartialAllocationsOnMismatch = false)
    {
        var result = new PaymentJournalEntryCreateResult();

        if (paymentId == Guid.Empty)
        {
            result.Bail("Exit: PaymentId is empty.");
            return result;
        }

        if (!await IsAccountingFeatureEnabledAsync(organizationId))
        {
            result.Bail("Exit: accounting feature is disabled for organization.");
            return result;
        }

        Payment? payment = null;
        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            payment = cachedPayment;
        else
            payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);

        if (payment == null)
        {
            result.Bail("Exit: payment record not found.");
            return result;
        }

        result.Note($"Payment {payment.PaymentCode} IsActive={payment.IsActive} Amount={payment.Amount:0.00} Lines={payment.LedgerLines.Count}");

        if (payment.LedgerLines.Count == 0)
        {
            result.Bail("Exit: payment has no linked invoice ledger lines.");
            return result;
        }

        // AGENT-NOTE: Payment JE create must not skip inactive invoices. LoadPaymentApplications
        // uses Invoice_GetById (IsDeleted=0 only). Invoice.IsActive is ignored here on purpose —
        // Sync and payment save both process payment lines on inactive invoices.
        var loadResult = await LoadPaymentApplicationsAsync(payment, organizationId, strict: !allowPartialAllocationsOnMismatch);
        result.Note($"LoadPaymentApplications: apps={loadResult.Applications.Count} skipped={loadResult.SkippedLines.Count} failed={loadResult.FailedLines.Count}");
        AppendPaymentLedgerLineIssuesToBail(result, "Skipped linked lines", loadResult.SkippedLines);
        AppendPaymentLedgerLineIssuesToBail(result, "Failed linked lines", loadResult.FailedLines);

        if (loadResult.Applications.Count == 0)
        {
            result.Bail("Exit: no payment applications resolved from linked ledger lines.");
            return result;
        }

        foreach (var application in loadResult.Applications)
        {
            result.Note(
                $"Application: invoice {application.Invoice.InvoiceCode} IsActive={application.Invoice.IsActive} "
                + $"line {application.PaymentLedgerLine.LineNumber} amount={application.PaymentLedgerLine.Amount:0.00} "
                + $"LedgerLineId={application.PaymentLedgerLine.LedgerLineId}");
        }

        var allocationTotal = loadResult.Applications.Sum(application => application.PaymentLedgerLine.Amount);
        if (allocationTotal != payment.Amount)
        {
            var diagnostic = BuildPaymentAllocationDiagnosticMessage(payment, loadResult, allocationTotal);
            result.Note($"Allocation mismatch: header={payment.Amount:0.00} applications={allocationTotal:0.00}");
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
            result.Note("Continuing with partial allocations (Sync allowPartialAllocationsOnMismatch).");
        }

        await CreateJournalEntriesFromPaymentApplicationsAsync(payment, loadResult.Applications, currentUser, result);

        if (result.JournalEntries.Count == 0)
            result.Bail("Exit: create/upsert finished with zero Payment/PrePaymentReceive journal entries returned.");
        else
            result.Note($"Create returned {result.JournalEntries.Count} journal entry(ies): "
                + string.Join(", ", result.JournalEntries.Select(entry =>
                    $"{entry.JournalEntryCode} kind={(int)entry.JournalEntryKindId} PaymentId={entry.PaymentId}")));

        return result;
    }

    public async Task<JournalEntrySyncResult> SyncPaymentJournalEntriesAsync(Guid organizationId, string officeIds, Guid currentUser, IProgress<JournalEntrySyncProgress>? progress = null, bool syncDocumentLinksAtEnd = true)
    {
        var result = new JournalEntrySyncResult();
        await ReconcileOrphanPaymentsDuringSyncAsync(organizationId, officeIds, currentUser, result);

        var invoicePayments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentKind.Invoice))
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.PaymentId)
            .ToList();

        var billPayments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentKind.Bill))
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.PaymentId)
            .ToList();

        var ownerPayments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentKind.Owner))
            .OrderBy(payment => payment.PaymentDate)
            .ThenBy(payment => payment.PaymentId)
            .ToList();

        var allPayments = invoicePayments.Concat(billPayments).Concat(ownerPayments).ToList();

        return await WithOfficeSyncCacheAsync(organizationId, officeIds, async () =>
        {
            var total = allPayments.Count;
            var processed = 0;
            ReportSyncProgress(progress, "payment", total, processed, result, "Running");

            foreach (var paymentSummary in invoicePayments)
            {
                result.DocumentsProcessed++;

                try
                {
                    var createResult = await CreateJournalEntriesFromInvoicePaymentDocumentWithDiagnosticsAsync(
                        paymentSummary.PaymentId,
                        organizationId,
                        currentUser,
                        allowPartialAllocationsOnMismatch: true);

                    // Health_01 bar — kind 13/14 with PaymentId. Loose memo matches do not count as success.
                    var hasHealthPaymentJe = await PaymentHasHealthPaymentJournalEntryAsync(paymentSummary.PaymentId, organizationId);
                    if (hasHealthPaymentJe)
                    {
                        result.JournalEntriesCreated++;
                    }
                    else
                    {
                        result.JournalEntriesSkipped++;
                        Payment? payment = null;
                        if (_officeSyncCache != null
                            && _officeSyncCache.PaymentsById.TryGetValue(paymentSummary.PaymentId, out var cachedPayment))
                        {
                            payment = cachedPayment;
                        }
                        else
                        {
                            payment = await _accountingRepository.GetPaymentByIdAsync(paymentSummary.PaymentId, organizationId);
                        }

                        var looseHasAny = await PaymentHasJournalEntriesAsync(paymentSummary.PaymentId, organizationId);
                        var skipMessage = await BuildPaymentSkipDiagnosticMessageAsync(payment, paymentSummary, organizationId);
                        var bailTrail = createResult.FormatBailTrail();
                        var message = skipMessage
                            + Environment.NewLine
                            + $"Health_01 Payment JE present: false. Loose PaymentHas (any memo/link match): {looseHasAny}."
                            + Environment.NewLine
                            + "Bail trail:"
                            + Environment.NewLine
                            + bailTrail;

                        await LogAccountingErrorAsync(
                            trigger: "PaymentSkip",
                            organizationId: organizationId,
                            officeId: paymentSummary.OfficeId,
                            sourceTypeId: (int)SourceType.InvoicePayment,
                            sourceId: paymentSummary.PaymentId,
                            documentCode: ResolvePaymentDocumentCode(payment, paymentSummary),
                            accountingPeriod: null,
                            amount: paymentSummary.Amount,
                            message: message,
                            currentUser: currentUser);
                        result.Errors.Add($"{ResolvePaymentDocumentCode(payment, paymentSummary)}: no Health Payment JE. See PaymentSkip log.");
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
                        documentCode: paymentSummary.PaymentCode ?? paymentSummary.Description,
                        accountingPeriod: null,
                        amount: paymentSummary.Amount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "payment", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            foreach (var paymentSummary in billPayments)
            {
                result.DocumentsProcessed++;

                try
                {
                    await SyncBillPaymentJournalEntryAsync(paymentSummary, organizationId, currentUser, result);
                }
                catch (Exception ex)
                {
                    var message = $"Bill payment {paymentSummary.PaymentCode}: {ex.Message}";
                    result.Errors.Add(message);
                    await LogAccountingErrorAsync(
                        trigger: "BillPayment",
                        organizationId: organizationId,
                        officeId: paymentSummary.OfficeId,
                        sourceTypeId: (int)SourceType.BillPayment,
                        sourceId: paymentSummary.PaymentId,
                        documentCode: paymentSummary.PaymentCode ?? paymentSummary.Description,
                        accountingPeriod: null,
                        amount: paymentSummary.Amount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "payment", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            foreach (var paymentSummary in ownerPayments)
            {
                result.DocumentsProcessed++;

                try
                {
                    await SyncOwnerPaymentJournalEntryAsync(paymentSummary, organizationId, currentUser, result);
                }
                catch (Exception ex)
                {
                    var message = $"Owner payment {paymentSummary.PaymentCode}: {ex.Message}";
                    result.Errors.Add(message);
                    await LogAccountingErrorAsync(
                        trigger: "OwnerPayment",
                        organizationId: organizationId,
                        officeId: paymentSummary.OfficeId,
                        sourceTypeId: (int)SourceType.OwnerDistribution,
                        sourceId: paymentSummary.PaymentId,
                        documentCode: paymentSummary.PaymentCode ?? paymentSummary.Description,
                        accountingPeriod: null,
                        amount: paymentSummary.Amount,
                        message: message,
                        currentUser: currentUser);
                }

                processed++;
                ReportSyncProgress(progress, "payment", total, processed, result, processed >= total ? "Completed" : "Running");
            }

            if (total == 0)
                ReportSyncProgress(progress, "payment", total, processed, result, "Completed");

            if (syncDocumentLinksAtEnd)
                await SyncDocumentLinksAsync(organizationId, officeIds, currentUser, progress);

            return result;
        }, paymentsAlreadyLoaded: allPayments);
    }
    #endregion

    #region Helpers
    private async Task CreateJournalEntriesFromPaymentApplicationsAsync(
        Payment payment,
        IReadOnlyList<PaymentApplicationContext> applications,
        Guid currentUser,
        PaymentJournalEntryCreateResult result)
    {
        var existingPaymentDocumentEntries = await GetJournalEntriesForSourceAsync(
            payment.OrganizationId,
            payment.OfficeId,
            SourceType.InvoicePayment,
            payment.PaymentId);
        result.Note($"Existing SourceType.InvoicePayment JEs for payment: {existingPaymentDocumentEntries.Count}");

        foreach (var application in applications)
        {
            var invoiceLabel = application.Invoice.InvoiceCode;
            var existingPaymentEntries = await GetJournalEntriesForInvoicePaymentLedgerLineAsync(
                application.Invoice.OrganizationId,
                application.Invoice.OfficeId,
                application.Invoice,
                application.PaymentLedgerLine);
            result.Note(
                $"Invoice {invoiceLabel}: memo-matched existing payment-flow JEs={existingPaymentEntries.Count} "
                + $"[{string.Join(", ", existingPaymentEntries.Select(entry => $"{entry.JournalEntryCode}/k{(int)entry.JournalEntryKindId}/PayId={entry.PaymentId}"))}]");

            var paymentResult = await UpsertInvoicePaymentSideEffectsAsync(
                application.Invoice,
                application.PaymentLedgerLine,
                existingPaymentEntries,
                currentUser,
                createMainCashJournalEntry: true);

            if (paymentResult.HasWarning)
                result.Bail($"Invoice {invoiceLabel}: UpsertInvoicePaymentSideEffects warning: {paymentResult.Warning}");

            if (paymentResult.JournalEntry == null)
            {
                result.Bail(
                    $"Invoice {invoiceLabel}: UpsertInvoicePaymentSideEffects returned no main Payment JE "
                    + $"(HasWarning={paymentResult.HasWarning}).");
                continue;
            }

            result.Note(
                $"Invoice {invoiceLabel}: upserted {paymentResult.JournalEntry.JournalEntryCode} "
                + $"kind={(int)paymentResult.JournalEntry.JournalEntryKindId} PaymentId={paymentResult.JournalEntry.PaymentId}");

            if (!result.JournalEntries.Any(entry => entry.JournalEntryId == paymentResult.JournalEntry.JournalEntryId))
                result.JournalEntries.Add(paymentResult.JournalEntry);
        }

        await SyncPaymentDocumentLinksAsync(payment, currentUser);
        result.Note("SyncPaymentDocumentLinksAsync completed.");
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

    private sealed class PaymentJournalEntryCreateResult
    {
        public List<JournalEntry> JournalEntries { get; } = [];
        private readonly AccountingSyncBailTrail _trail = new();

        public void Note(string message) => _trail.Note(message);

        public void Bail(string message) => _trail.Bail(message);

        public string FormatBailTrail() => _trail.FormatBailTrail();
    }

    private static void AppendPaymentLedgerLineIssuesToBail(
        PaymentJournalEntryCreateResult result,
        string heading,
        IReadOnlyList<PaymentLedgerLineLoadIssue> issues)
    {
        if (issues.Count == 0)
            return;

        result.Note($"{heading}:");
        foreach (var issue in issues)
        {
            result.Note(
                $"  - {issue.Line.InvoiceCode} line {issue.Line.LineNumber}: {issue.Line.Amount:0.00} ({issue.Line.Description}) "
                + $"[LedgerLineId={issue.Line.LedgerLineId}] — {issue.Reason}");
        }
    }

    /// <summary>Health_01: kind Payment/PrePaymentReceive with PaymentId stamped.</summary>
    private async Task<bool> PaymentHasHealthPaymentJournalEntryAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return false;

        var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(organizationId, paymentId);
        return linkedEntries.Any(entry =>
            entry.JournalEntryKindId is JournalEntryKind.Payment or JournalEntryKind.PrePaymentReceive
            && entry.PaymentId is { } linkedPaymentId
            && linkedPaymentId != Guid.Empty);
    }

    private async Task<bool> PaymentHasJournalEntriesAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return false;

        var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(organizationId, paymentId);
        if (linkedEntries.Any())
            return true;

        Payment? payment = null;
        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            payment = cachedPayment;
        else
            payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null)
            return false;

        foreach (var paymentLine in payment.LedgerLines.Where(line => line.Amount != 0))
        {
            if (paymentLine.InvoiceId == Guid.Empty)
                continue;

            Invoice? invoice = null;
            if (_officeSyncCache == null
                || !_officeSyncCache.TryGetInvoiceWithLedgerLines(paymentLine.InvoiceId, out invoice))
            {
                invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, organizationId);
            }

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

        // Prefer office sync cache — GetPaymentById used to INNER JOIN CostCode/User and return null
        // for real payments, which aborted Payment JE create with "Payment record not found".
        Payment? payment = null;
        if (_officeSyncCache != null && _officeSyncCache.PaymentsById.TryGetValue(paymentId, out var cachedPayment))
            payment = cachedPayment;
        else
            payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);

        if (payment == null)
            throw new Exception($"Payment record not found for PaymentId {paymentId}.");

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
        if (payment.PaymentId == Guid.Empty)
            return;

        if (!await IsAccountingFeatureEnabledAsync(payment.OrganizationId))
            return;

        await EnsurePaymentCodePersistedAsync(payment, currentUser);

        await ClearPaymentDocumentLinksAsync(payment.OrganizationId, payment.PaymentId, currentUser);

        var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(payment.OrganizationId, payment.PaymentId);

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

            Invoice? invoice = null;
            if (_officeSyncCache == null
                || !_officeSyncCache.TryGetInvoiceWithLedgerLines(paymentLine.InvoiceId, out invoice))
            {
                invoice = await _accountingRepository.GetInvoiceByIdAsync(paymentLine.InvoiceId, payment.OrganizationId);
            }

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

        var linkedEntries = await GetJournalEntriesByPaymentIdCachedAsync(organizationId, paymentId);

        foreach (var journalEntry in linkedEntries)
        {
            ClearPaymentDocumentLink(journalEntry);
            journalEntry.ModifiedBy = currentUser;
            await UpdateJournalEntryWithoutRetainedEarningsRefreshAsync(journalEntry, requireActiveLines: true);
        }
    }
    #endregion
}
