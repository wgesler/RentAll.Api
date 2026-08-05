using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Text;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private sealed record UnlinkedPaymentLineGroupKey(Guid OrganizationId, int OfficeId, DateOnly PaymentDate, int CostCodeId, string Description, Guid CreatedBy, DateTimeOffset CreatedOnBatch);

    private sealed class UnlinkedPaymentLineGroup
    {
        public UnlinkedPaymentLineGroupKey Key { get; init; } = null!;
        public decimal TotalAmount { get; set; }
        public List<UnlinkedPaymentLineContext> Lines { get; init; } = [];
    }

    private sealed record UnlinkedPaymentLineContext(Invoice Invoice, LedgerLine Line);

    private async Task ReconcileOrphanPaymentsDuringSyncAsync(Guid organizationId, string officeIds, Guid currentUser, JournalEntrySyncResult result)
    {
        var groups = await BuildUnlinkedPaymentLineGroupsAsync(organizationId, officeIds);
        if (groups.Count == 0)
            return;

        var orphanPayments = (await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds))
            .Where(payment => payment.IsActive && payment.LedgerLines.Count == 0)
            .ToList();

        var candidateMatches = new List<(UnlinkedPaymentLineGroup Group, Payment Orphan)>();
        foreach (var group in groups)
        {
            foreach (var orphan in orphanPayments.Where(payment =>
                         payment.OrganizationId == group.Key.OrganizationId
                         && payment.OfficeId == group.Key.OfficeId
                         && payment.PaymentDate == group.Key.PaymentDate
                         && payment.CostCodeId == group.Key.CostCodeId
                         && payment.Description == group.Key.Description
                         && payment.Amount == group.TotalAmount))
            {
                candidateMatches.Add((group, orphan));
            }
        }

        var reconnectMatches = candidateMatches
            .GroupBy(match => match.Group)
            .Where(groupMatches => groupMatches.Count() == 1
                && candidateMatches.Count(match => match.Orphan.PaymentId == groupMatches.First().Orphan.PaymentId) == 1)
            .Select(groupMatches => groupMatches.First())
            .ToList();

        foreach (var (group, orphan) in reconnectMatches)
        {
            foreach (var context in group.Lines)
            {
                await _accountingRepository.SetLedgerLinePaymentIdAsync(context.Line.LedgerLineId, orphan.PaymentId, currentUser);
                context.Line.PaymentId = orphan.PaymentId;
            }

            result.DocumentsProcessed++;
            await LogPaymentReconnectDuringSyncAsync(orphan, group, currentUser);
        }

        var reconnectedGroups = reconnectMatches.Select(match => match.Group).ToHashSet();
        var remainingGroups = groups.Where(group => !reconnectedGroups.Contains(group)).ToList();

        foreach (var ambiguous in candidateMatches
                     .GroupBy(match => match.Group)
                     .Where(groupMatches => groupMatches.Count() > 1))
        {
            await LogPaymentReconnectSkippedAsync(
                organizationId,
                ambiguous.Key.Key.OfficeId,
                ambiguous.Key.TotalAmount,
                ambiguous.Key.Key.PaymentDate,
                BuildAmbiguousGroupReconnectMessage(ambiguous.Key, ambiguous.Select(match => match.Orphan).ToList()),
                currentUser);
            result.Errors.Add($"Payment reconnect skipped (ambiguous group): {ambiguous.Key.Lines.First().Invoice.InvoiceCode}");
        }

        foreach (var orphan in orphanPayments.Where(payment => !reconnectMatches.Any(match => match.Orphan.PaymentId == payment.PaymentId)))
        {
            if (candidateMatches.Any(match => match.Orphan.PaymentId == orphan.PaymentId))
                continue;

            await LogAccountingErrorAsync(
                trigger: "PaymentReconcileOrphan",
                organizationId: organizationId,
                officeId: orphan.OfficeId,
                sourceTypeId: (int)SourceType.InvoicePayment,
                sourceId: orphan.PaymentId,
                documentCode: orphan.PaymentCode,
                accountingPeriod: null,
                amount: orphan.Amount,
                message: BuildOrphanPaymentWithoutMatchMessage(orphan),
                currentUser: currentUser);
        }

        if (remainingGroups.Count > 0)
            await LogRemainingUnlinkedPaymentLinesDuringSyncAsync(organizationId, remainingGroups, currentUser, result);
    }

    private async Task<List<UnlinkedPaymentLineGroup>> BuildUnlinkedPaymentLineGroupsAsync(Guid organizationId, string officeIds)
    {
        var groupsByKey = new Dictionary<UnlinkedPaymentLineGroupKey, UnlinkedPaymentLineGroup>();

        foreach (var invoiceSummary in await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            IncludeInactive = true,
            IncludePaid = true
        }))
        {
            var invoice = await _accountingRepository.GetInvoiceByIdAsync(invoiceSummary.InvoiceId, organizationId);
            if (invoice == null)
                continue;

            var costCodeById = await LoadCostCodeByOfficeIdAsync(organizationId, invoice.OfficeId);
            foreach (var line in invoice.LedgerLines.Where(line => line.Amount != 0))
            {
                if (!costCodeById.TryGetValue(line.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                    continue;

                if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                    continue;

                var key = new UnlinkedPaymentLineGroupKey(
                    invoice.OrganizationId,
                    invoice.OfficeId,
                    line.LedgerLineDate,
                    line.CostCodeId,
                    line.Description,
                    line.CreatedBy,
                    TruncateToSecond(line.CreatedOn));

                if (!groupsByKey.TryGetValue(key, out var group))
                {
                    group = new UnlinkedPaymentLineGroup { Key = key };
                    groupsByKey[key] = group;
                }

                group.TotalAmount += line.Amount;
                group.Lines.Add(new UnlinkedPaymentLineContext(invoice, line));
            }
        }

        return groupsByKey.Values.ToList();
    }

    private async Task LogPaymentReconnectDuringSyncAsync(Payment orphan, UnlinkedPaymentLineGroup group, Guid currentUser)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Payment ledger lines were reconnected to an existing orphan payment header during sync.");
        builder.AppendLine($"PaymentId: {orphan.PaymentId}");
        builder.AppendLine($"PaymentCode: {orphan.PaymentCode}");
        builder.AppendLine($"PaymentDate: {orphan.PaymentDate:yyyy-MM-dd}");
        builder.AppendLine($"PaymentAmount: {orphan.Amount:0.00}");
        builder.AppendLine($"Description: {orphan.Description}");
        builder.AppendLine($"ReconnectedLines: {group.Lines.Count}");
        builder.AppendLine("Lines:");
        foreach (var context in group.Lines.OrderBy(context => context.Invoice.InvoiceCode).ThenBy(context => context.Line.LineNumber))
        {
            builder.AppendLine(
                $"  - Invoice {context.Invoice.InvoiceCode} line {context.Line.LineNumber}: {context.Line.Amount:0.00} ({context.Line.Description}) [LedgerLineId={context.Line.LedgerLineId}]");
        }

        builder.Append(
            "Match criteria: organization, office, payment date, cost code, description, and total amount were a unique orphan payment match.");

        var firstInvoice = group.Lines.First().Invoice;
        await LogAccountingErrorAsync(
            trigger: "PaymentReconnect",
            organizationId: orphan.OrganizationId,
            officeId: orphan.OfficeId,
            sourceTypeId: (int)SourceType.InvoicePayment,
            sourceId: orphan.PaymentId,
            documentCode: orphan.PaymentCode,
            accountingPeriod: firstInvoice.AccountingPeriod == default ? null : firstInvoice.AccountingPeriod,
            amount: orphan.Amount,
            message: builder.ToString().Trim(),
            currentUser: currentUser);
    }

    private async Task LogPaymentReconnectSkippedAsync(Guid organizationId, int officeId, decimal amount, DateOnly paymentDate, string message, Guid currentUser)
    {
        await LogAccountingErrorAsync(
            trigger: "PaymentReconnectSkip",
            organizationId: organizationId,
            officeId: officeId,
            sourceTypeId: (int)SourceType.InvoicePayment,
            sourceId: null,
            documentCode: null,
            accountingPeriod: paymentDate,
            amount: amount,
            message: message,
            currentUser: currentUser);
    }

    private async Task LogRemainingUnlinkedPaymentLinesDuringSyncAsync(Guid organizationId, IReadOnlyList<UnlinkedPaymentLineGroup> groups, Guid currentUser, JournalEntrySyncResult result)
    {
        foreach (var group in groups)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Unlinked payment ledger line(s) remain after sync reconnect. This should not happen on current invoice-save code.");
            builder.AppendLine($"OfficeId: {group.Key.OfficeId}");
            builder.AppendLine($"PaymentDate: {group.Key.PaymentDate:yyyy-MM-dd}");
            builder.AppendLine($"GroupAmount: {group.TotalAmount:0.00}");
            builder.AppendLine($"Description: {group.Key.Description}");
            builder.AppendLine($"LineCount: {group.Lines.Count}");
            builder.AppendLine("Lines:");
            foreach (var context in group.Lines.OrderBy(context => context.Invoice.InvoiceCode).ThenBy(context => context.Line.LineNumber))
            {
                builder.AppendLine(
                    $"  - Invoice {context.Invoice.InvoiceCode} line {context.Line.LineNumber}: {context.Line.Amount:0.00} ({context.Line.Description}) [LedgerLineId={context.Line.LedgerLineId}]");
            }

            builder.Append("Action: investigate how the payment link was lost, or run the manual payment fix script.");

            var firstInvoice = group.Lines.First().Invoice;
            result.Errors.Add($"Unlinked payment lines on invoice {firstInvoice.InvoiceCode}");
            await LogAccountingErrorAsync(
                trigger: "PaymentReconcileUnlinked",
                organizationId: organizationId,
                officeId: group.Key.OfficeId,
                sourceTypeId: (int)SourceType.Invoice,
                sourceId: firstInvoice.InvoiceId,
                documentCode: firstInvoice.InvoiceCode,
                accountingPeriod: firstInvoice.AccountingPeriod == default ? null : firstInvoice.AccountingPeriod,
                amount: group.TotalAmount,
                message: builder.ToString().Trim(),
                currentUser: currentUser);
        }
    }

    private static string BuildAmbiguousGroupReconnectMessage(UnlinkedPaymentLineGroup group, IReadOnlyList<Payment> orphanMatches)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Payment reconnect skipped because multiple orphan payment headers matched the same ledger-line group.");
        builder.AppendLine($"OfficeId: {group.Key.OfficeId}");
        builder.AppendLine($"PaymentDate: {group.Key.PaymentDate:yyyy-MM-dd}");
        builder.AppendLine($"GroupAmount: {group.TotalAmount:0.00}");
        builder.AppendLine($"Description: {group.Key.Description}");
        builder.AppendLine($"Matching orphan payments: {orphanMatches.Count}");
        foreach (var orphan in orphanMatches.OrderBy(payment => payment.PaymentCode))
            builder.AppendLine($"  - {orphan.PaymentCode} ({orphan.PaymentId}) amount {orphan.Amount:0.00}");

        builder.Append("Action: manual review required.");
        return builder.ToString().Trim();
    }

    private static string BuildOrphanPaymentWithoutMatchMessage(Payment orphan)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Orphan payment header has no linked invoice ledger lines and no unique unlinked line group matched during sync.");
        builder.AppendLine($"PaymentId: {orphan.PaymentId}");
        builder.AppendLine($"PaymentCode: {orphan.PaymentCode}");
        builder.AppendLine($"PaymentDate: {orphan.PaymentDate:yyyy-MM-dd}");
        builder.AppendLine($"Amount: {orphan.Amount:0.00}");
        builder.AppendLine($"Description: {orphan.Description}");
        builder.Append("Action: manual review or delete if duplicate.");
        return builder.ToString().Trim();
    }

    private static DateTimeOffset TruncateToSecond(DateTimeOffset value)
        => new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

    private async Task ValidateIncomingInvoicePaymentLedgerLinesAsync(Invoice invoice)
    {
        if (invoice.LedgerLines.Count == 0)
            return;

        var costCodeById = await LoadCostCodeByOfficeIdAsync(invoice.OrganizationId, invoice.OfficeId);
        foreach (var line in invoice.LedgerLines.Where(line => line.Amount != 0))
        {
            if (!costCodeById.TryGetValue(line.CostCodeId, out var costCode) || !IsPaymentLedgerLine(costCode))
                continue;

            if (line.PaymentId is { } paymentId && paymentId != Guid.Empty)
                continue;

            throw new InvalidOperationException(
                "A payment line on this invoice is not linked to a payment record. To apply a payment, use Apply Payment on the invoice list. To remove a payment, delete it from the Payment list — do not add or remove payment lines on the invoice.");
        }
    }
}
