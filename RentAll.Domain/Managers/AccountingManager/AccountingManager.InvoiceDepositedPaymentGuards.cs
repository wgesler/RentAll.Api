using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    #region Invoice Deposited Payment Guards
    private sealed record DepositedPaymentLockContext(Payment Payment, Deposit? Deposit);

    private async Task ValidateInvoiceUpdatePreservesDepositedPaymentsAsync(Invoice incoming, Invoice existing)
    {
        if (incoming.InvoiceId == Guid.Empty || existing.InvoiceId == Guid.Empty)
            return;

        var costCodeById = await LoadCostCodeByOfficeIdAsync(existing.OrganizationId, existing.OfficeId);
        var incomingLinesById = incoming.LedgerLines
            .Where(line => line.LedgerLineId != Guid.Empty)
            .ToDictionary(line => line.LedgerLineId);

        foreach (var existingLine in GetInvoicePaymentLedgerLines(existing, costCodeById))
        {
            if (existingLine.PaymentId is not { } paymentId || paymentId == Guid.Empty)
                continue;

            var lockContext = await TryLoadDepositedPaymentLockContextAsync(paymentId, existing.OrganizationId);
            if (lockContext == null)
                continue;

            if (!incomingLinesById.TryGetValue(existingLine.LedgerLineId, out var incomingLine))
            {
                throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
                    existing.InvoiceCode,
                    lockContext,
                    "The payment line must remain on the invoice because the payment has already been deposited."));
            }

            if (PaymentLedgerLineMetadataChanged(existingLine, incomingLine))
            {
                throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
                    existing.InvoiceCode,
                    lockContext,
                    "Payment amount, date, cost code, or description cannot be changed on the invoice after deposit."));
            }

            if (incomingLine.PaymentId is { } incomingPaymentId
                && incomingPaymentId != Guid.Empty
                && incomingPaymentId != paymentId)
            {
                throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
                    existing.InvoiceCode,
                    lockContext,
                    "The payment document link cannot be changed on the invoice after deposit."));
            }
        }

        await ValidateNoDuplicatePaymentLinesForDepositedPaymentsAsync(incoming, existing, costCodeById);
    }

    private async Task ValidateNoDuplicatePaymentLinesForDepositedPaymentsAsync(Invoice incoming, Invoice existing, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var depositedLines = new List<(LedgerLine Line, DepositedPaymentLockContext LockContext)>();
        foreach (var existingLine in GetInvoicePaymentLedgerLines(existing, costCodeById))
        {
            if (existingLine.PaymentId is not { } paymentId || paymentId == Guid.Empty)
                continue;

            var lockContext = await TryLoadDepositedPaymentLockContextAsync(paymentId, existing.OrganizationId);
            if (lockContext != null)
                depositedLines.Add((existingLine, lockContext));
        }

        if (depositedLines.Count == 0)
            return;

        foreach (var incomingLine in GetInvoicePaymentLedgerLines(incoming, costCodeById))
        {
            if (incomingLine.LedgerLineId != Guid.Empty)
                continue;

            foreach (var (depositedLine, lockContext) in depositedLines)
            {
                if (depositedLine.Amount != incomingLine.Amount
                    || depositedLine.LedgerLineDate != incomingLine.LedgerLineDate
                    || depositedLine.CostCodeId != incomingLine.CostCodeId)
                {
                    continue;
                }

                throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
                    existing.InvoiceCode,
                    lockContext,
                    "A new payment line cannot duplicate an already-deposited payment. Keep the existing payment line on the invoice."));
            }
        }
    }

    private async Task ValidateNewPaymentLinesAgainstDepositedPaymentsAsync(Invoice invoice)
    {
        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        var depositedLines = new List<(LedgerLine Line, DepositedPaymentLockContext LockContext)>();
        foreach (var line in GetInvoicePaymentLedgerLines(invoice, costCodeById))
        {
            if (line.PaymentId is not { } paymentId || paymentId == Guid.Empty)
                continue;

            var lockContext = await TryLoadDepositedPaymentLockContextAsync(paymentId, invoice.OrganizationId);
            if (lockContext != null)
                depositedLines.Add((line, lockContext));
        }

        if (depositedLines.Count == 0)
            return;

        foreach (var line in GetInvoicePaymentLedgerLines(invoice, costCodeById))
        {
            if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                continue;

            foreach (var (depositedLine, lockContext) in depositedLines)
            {
                if (depositedLine.Amount != line.Amount
                    || depositedLine.LedgerLineDate != line.LedgerLineDate
                    || depositedLine.CostCodeId != line.CostCodeId)
                {
                    continue;
                }

                throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
                    invoice.InvoiceCode,
                    lockContext,
                    "A new payment line cannot duplicate an already-deposited payment. Keep the existing payment line on the invoice."));
            }
        }
    }

    private void EnsureDepositedPaymentHeaderUnchanged(Payment payment, DepositedPaymentLockContext lockContext, decimal linkedTotal, LedgerLine? metadataSource, string invoiceCode)
    {
        if (payment.DepositId is not { } depositId || depositId == Guid.Empty)
            return;

        var changed = payment.Amount != linkedTotal;
        if (metadataSource != null)
        {
            changed = changed
                || payment.PaymentDate != metadataSource.LedgerLineDate
                || payment.CostCodeId != metadataSource.CostCodeId
                || !string.Equals(payment.Description, metadataSource.Description, StringComparison.Ordinal);
        }

        if (!changed)
            return;

        throw new InvalidOperationException(BuildDepositedPaymentLockedMessage(
            invoiceCode,
            lockContext,
            "The linked payment document cannot be changed from the invoice after deposit."));
    }

    private async Task<HashSet<Guid>> CollectDepositedPaymentIdsAsync(Invoice invoice, Invoice? priorInvoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        var paymentIds = new HashSet<Guid>();
        foreach (var line in GetInvoicePaymentLedgerLines(invoice, costCodeById))
        {
            if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                paymentIds.Add(paymentId);
        }

        if (priorInvoice != null)
        {
            foreach (var line in GetInvoicePaymentLedgerLines(priorInvoice, costCodeById))
            {
                if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                    paymentIds.Add(paymentId);
            }
        }

        var depositedPaymentIds = new HashSet<Guid>();
        foreach (var paymentId in paymentIds)
        {
            if (await TryLoadDepositedPaymentLockContextAsync(paymentId, invoice.OrganizationId) != null)
                depositedPaymentIds.Add(paymentId);
        }

        return depositedPaymentIds;
    }

    private async Task<DepositedPaymentLockContext?> TryLoadDepositedPaymentLockContextAsync(Guid paymentId, Guid organizationId)
    {
        if (paymentId == Guid.Empty)
            return null;

        var payment = await _accountingRepository.GetPaymentByIdAsync(paymentId, organizationId);
        if (payment == null || payment.DepositId is not { } depositId || depositId == Guid.Empty)
            return null;

        var deposit = await _accountingRepository.GetDepositByIdAsync(depositId, organizationId);
        return new DepositedPaymentLockContext(payment, deposit);
    }

    private static IEnumerable<LedgerLine> GetInvoicePaymentLedgerLines(Invoice invoice, IReadOnlyDictionary<int, CostCode> costCodeById)
    {
        return invoice.LedgerLines
            .Where(line => line.Amount != 0)
            .Where(line => costCodeById.TryGetValue(line.CostCodeId, out var costCode) && IsPaymentLedgerLine(costCode));
    }

    private static bool PaymentLedgerLineMetadataChanged(LedgerLine existingLine, LedgerLine incomingLine)
    {
        return existingLine.Amount != incomingLine.Amount
            || existingLine.LedgerLineDate != incomingLine.LedgerLineDate
            || existingLine.CostCodeId != incomingLine.CostCodeId
            || !string.Equals(existingLine.Description, incomingLine.Description, StringComparison.Ordinal);
    }

    private static string BuildDepositedPaymentLockedMessage(string invoiceCode, DepositedPaymentLockContext lockContext, string detail)
    {
        var paymentLabel = string.IsNullOrWhiteSpace(lockContext.Payment.PaymentCode)
            ? lockContext.Payment.PaymentId.ToString()
            : lockContext.Payment.PaymentCode.Trim();
        var depositLabel = !string.IsNullOrWhiteSpace(lockContext.Payment.DepositCode)
            ? lockContext.Payment.DepositCode.Trim()
            : lockContext.Deposit?.DepositCode?.Trim() ?? lockContext.Payment.DepositId?.ToString() ?? string.Empty;
        var transferSuffix = lockContext.Deposit?.TransferId is { } transferId && transferId != Guid.Empty
            ? $" Transfer {(!string.IsNullOrWhiteSpace(lockContext.Deposit.TransferCode) ? lockContext.Deposit.TransferCode.Trim() : transferId.ToString())}."
            : string.Empty;

        return $"Invoice {invoiceCode}: payment {paymentLabel} is linked to deposit {depositLabel}.{transferSuffix} {detail}";
    }
    #endregion
}
