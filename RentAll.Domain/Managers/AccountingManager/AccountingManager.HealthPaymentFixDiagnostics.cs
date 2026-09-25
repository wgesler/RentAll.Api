using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using Microsoft.Extensions.Logging;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Health Payment Fix Diagnostics
    private const string HealthPaymentFixTracePrefix = "[HealthPaymentFixTrace]";

    private void LogHealthTransferFixTrace(
        Transfer transfer,
        string step,
        string reason,
        string? detail = null)
    {
        var transferCode = string.IsNullOrWhiteSpace(transfer.TransferCode)
            ? transfer.TransferId.ToString()
            : transfer.TransferCode.Trim();
        _logger.LogError(
            "{Prefix} Step={Step} TransferCode={TransferCode} TransferId={TransferId} Reason={Reason}{Detail}",
            HealthPaymentFixTracePrefix,
            step,
            transferCode,
            transfer.TransferId,
            reason,
            string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}");
    }

    private void LogHealthPaymentFixTrace(
        Payment payment,
        string step,
        string reason,
        string? detail = null)
    {
        var paymentCode = string.IsNullOrWhiteSpace(payment.PaymentCode)
            ? payment.PaymentId.ToString()
            : payment.PaymentCode.Trim();
        _logger.LogError(
            "{Prefix} Step={Step} PaymentCode={PaymentCode} PaymentId={PaymentId} Reason={Reason}{Detail}",
            HealthPaymentFixTracePrefix,
            step,
            paymentCode,
            payment.PaymentId,
            reason,
            string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}");
    }

    private async Task LogHealthPaymentFixFailureAsync(
        Payment payment,
        Guid currentUser,
        string step,
        string reason,
        string definitiveAction,
        string? detail = null)
    {
        var paymentCode = string.IsNullOrWhiteSpace(payment.PaymentCode)
            ? payment.PaymentId.ToString()
            : payment.PaymentCode.Trim();
        var message = string.Join(
            Environment.NewLine,
            new[]
            {
                $"{HealthPaymentFixTracePrefix} Step={step}",
                $"PaymentCode={paymentCode}",
                $"PaymentId={payment.PaymentId}",
                $"DefinitiveAction={definitiveAction}",
                $"Reason={reason}",
                detail
            }.Where(line => !string.IsNullOrWhiteSpace(line)));

        _logger.LogError(message);

        await LogAccountingErrorAsync(
            trigger: "HealthPaymentFix",
            organizationId: payment.OrganizationId,
            officeId: payment.OfficeId,
            sourceTypeId: (int)SourceType.InvoicePayment,
            sourceId: payment.PaymentId,
            documentCode: paymentCode,
            accountingPeriod: null,
            amount: payment.Amount,
            message: message,
            currentUser: currentUser);
    }

    private static string BuildHealthPaymentFixError(string paymentCode, string definitiveAction, string reason)
        => $"{paymentCode}: {definitiveAction} — {reason}";

    private async Task<string?> ClassifyOrphanInvoicePaymentActionAsync(Payment payment, Guid organizationId)
    {
        if (payment.LedgerLines.Count > 0)
            return null;

        var officeIds = payment.OfficeId.ToString();
        var groups = await BuildUnlinkedPaymentLineGroupsAsync(organizationId, officeIds);
        var matches = groups.Where(group =>
                group.Key.OrganizationId == payment.OrganizationId
                && group.Key.OfficeId == payment.OfficeId
                && group.Key.PaymentDate == payment.PaymentDate
                && group.Key.CostCodeId == payment.CostCodeId
                && group.Key.Description == payment.Description
                && group.TotalAmount == payment.Amount)
            .ToList();

        if (matches.Count == 0)
            return "DELETE this payment — no invoice ledger lines link to it and none could be matched for reconnect.";

        if (matches.Count > 1)
            return "MANUAL REVIEW — multiple unlinked invoice line groups match this orphan payment; pick the correct link or delete duplicates.";

        return "RE-RUN FIX — one unlinked invoice line group matches; orphan reconnect should link it (if Fix already ran, check Accounting Log for PaymentReconnect).";
    }

    private static string MapDepositSplitReconcileBailToAction(Payment payment, Deposit deposit, AccountingSyncBailTrail trail)
    {
        var trailText = trail.FormatBailTrail();
        if (trailText.Contains("Payment month is after deposit month", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "Fix payment/deposit dates or re-run Fix to unlink", trailText);

        if (trailText.Contains("No UF lines on payment journal entries", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "Fix payment JE first (missing or no UF line)", trailText);

        if (trailText.Contains("UF line count matching payment amount is", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "MANUAL REVIEW — UF line count/amount on payment JE does not uniquely match payment", trailText);

        if (trailText.Contains("claimed by another deposit", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "MANUAL REVIEW — UF line is linked to a different deposit", trailText);

        if (trailText.Contains("Deposit has no splits", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, $"Open deposit {deposit.DepositCode} and resave to rebuild splits", trailText);

        if (trailText.Contains("No default Undeposited Funds account", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "Configure Default Undeposited Funds for this office", trailText);

        if (trailText.Contains("No deposit split changes applied", StringComparison.OrdinalIgnoreCase))
            return BuildHealthPaymentFixError(payment.PaymentCode, "Run Deposits Fix — deposit split amounts/rules blocked auto-link", trailText);

        return BuildHealthPaymentFixError(
            payment.PaymentCode,
            $"Run Deposits Fix — no deposit split links this payment JE on {deposit.DepositCode}",
            trailText);
    }

    private static string MapPaymentJeCreateBailToAction(Payment payment, PaymentJournalEntryCreateResult createResult)
    {
        var trail = createResult.FormatBailTrail();
        if (trail.Contains("no linked invoice ledger lines", StringComparison.OrdinalIgnoreCase)
            || trail.Contains("no payment applications resolved", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHealthPaymentFixError(
                payment.PaymentCode,
                "DELETE or re-enter on invoice — payment header has no usable invoice ledger line links",
                trail);
        }

        if (trail.Contains("UpsertInvoicePaymentSideEffects warning", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHealthPaymentFixError(
                payment.PaymentCode,
                "Open payment on invoice and resave — invoice payment side-effects blocked JE create",
                trail);
        }

        if (trail.Contains("Consolidated Payment JE upsert returned null", StringComparison.OrdinalIgnoreCase)
            || trail.Contains("zero Payment/PrePaymentReceive journal entries", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHealthPaymentFixError(
                payment.PaymentCode,
                "MANUAL REVIEW — payment JE create returned no entry",
                trail);
        }

        return BuildHealthPaymentFixError(
            payment.PaymentCode,
            "MANUAL REVIEW — payment JE create failed",
            trail);
    }
    #endregion
}
