using System.Globalization;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private enum OwnerReportActivityMode
    {
        Accrual,
        Cash
    }

    private static List<OwnerStatementPropertyActivityLine> BuildOwnerReportPropertyActivityLines(
        IEnumerable<JournalEntryRecapLine> activityLines,
        IEnumerable<JournalEntryRecapLine> invoiceContextLines,
        OwnerReportActivityMode mode)
    {
        var allLines = (activityLines ?? []).Concat(invoiceContextLines ?? []).ToList();
        var invoiceContextByKey = BuildOwnerAccrualInvoiceContextByKey(invoiceContextLines);
        var prepaymentPaymentSourceIds = BuildOwnerReportPrepaymentPaymentSourceIds(allLines);
        var invoiceAccountingPeriodByKey = BuildOwnerReportInvoiceAccountingPeriodByKey(allLines);
        var groups = new Dictionary<string, OwnerAccrualSourceGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in activityLines ?? [])
        {
            if (!TryResolveOwnerAccrualPropertyId(line, out var propertyId))
                continue;

            var category = (line.RecapCategory ?? string.Empty).Trim();
            if (!IsOwnerAccrualRecapCategory(category))
                continue;

            if (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount > 0)
                continue;

            if (IsOwnerReportPrepaymentPaymentLine(
                line,
                prepaymentPaymentSourceIds,
                invoiceAccountingPeriodByKey))
                continue;

            var groupKey = BuildOwnerAccrualSourceGroupKey(line, category);
            if (!groups.TryGetValue(groupKey, out var group))
            {
                var invoiceSourceCode = ResolveOwnerReportIncomeInvoiceSourceKey(line, category);
                group = new OwnerAccrualSourceGroup
                {
                    PropertyId = propertyId,
                    OfficeId = line.OfficeId,
                    InvoiceSourceCode = invoiceSourceCode,
                    AccountingPeriod = line.AccountingPeriod.ToString("yyyy-MM-dd"),
                    SourceDocumentCode = ResolveRecapSourceDocumentCode(line),
                    TransactionDate = line.TransactionDate.ToString("yyyy-MM-dd"),
                    SortDateValue = line.TransactionDate.ToDateTime(TimeOnly.MinValue).Ticks
                };
                groups[groupKey] = group;
            }

            ApplyOwnerAccrualRecapLine(group, line, category);
            TouchOwnerAccrualSourceGroupMetadata(group, line);
        }

        return groups.Values
            .Where(HasOwnerAccrualSourceGroupActivity)
            .SelectMany(group => ExpandOwnerAccrualSourceGroupActivityLines(
                group,
                BuildOwnerAccrualInvoiceContextKey(
                    group.PropertyId,
                    group.InvoiceSourceCode ?? group.SourceDocumentCode,
                    string.Empty),
                invoiceContextByKey,
                mode))
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId)
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => ResolveOwnerAccrualActivitySortOrder(line))
            .ThenBy(line => line.AccountingPeriod, StringComparer.Ordinal)
            .ThenBy(line => line.DocumentCode, StringComparer.Ordinal)
            .ToList();
    }

    private static string ResolveOwnerReportIncomeInvoiceSourceKey(JournalEntryRecapLine line, string category)
    {
        if (string.Equals(category, "Payment", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase) && line.Amount < 0))
        {
            foreach (var invoiceCode in ResolveRecapPaymentInvoiceSourceCodes(line))
                return invoiceCode;
        }

        var sourceDocumentCode = ResolveRecapSourceDocumentCode(line);
        if (!string.IsNullOrWhiteSpace(sourceDocumentCode))
            return sourceDocumentCode;

        if (line.SourceId.HasValue && line.SourceId.Value != Guid.Empty)
            return line.SourceId.Value.ToString("D");

        return line.JournalEntryLineId.ToString("D");
    }

    private static HashSet<Guid> BuildOwnerReportPrepaymentPaymentSourceIds(IEnumerable<JournalEntryRecapLine> lines)
    {
        var sourceIds = new HashSet<Guid>();
        foreach (var line in lines ?? [])
        {
            if (!string.Equals(line.RecapCategory, "PrePayment", StringComparison.OrdinalIgnoreCase)
                || line.Amount <= 0
                || !line.SourceId.HasValue
                || line.SourceId.Value == Guid.Empty)
            {
                continue;
            }

            sourceIds.Add(line.SourceId.Value);
        }

        return sourceIds;
    }

    private static Dictionary<string, DateOnly> BuildOwnerReportInvoiceAccountingPeriodByKey(IEnumerable<JournalEntryRecapLine> lines)
    {
        var periodByKey = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines ?? [])
        {
            if (!TryResolveOwnerAccrualPropertyId(line, out var propertyId))
                continue;

            if (!string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
                continue;

            var contextKey = BuildOwnerAccrualInvoiceContextKey(
                propertyId,
                ResolveRecapSourceDocumentCode(line),
                string.Empty);

            if (!periodByKey.TryGetValue(contextKey, out var existingPeriod)
                || line.AccountingPeriod < existingPeriod)
            {
                periodByKey[contextKey] = line.AccountingPeriod;
            }
        }

        return periodByKey;
    }

    private static bool IsOwnerReportPrepaymentPaymentLine(
        JournalEntryRecapLine line,
        IReadOnlySet<Guid> prepaymentPaymentSourceIds,
        IReadOnlyDictionary<string, DateOnly> invoiceAccountingPeriodByKey)
    {
        if (!string.Equals(line.RecapCategory, "Payment", StringComparison.OrdinalIgnoreCase))
            return false;

        if (line.SourceId.HasValue
            && line.SourceId.Value != Guid.Empty
            && prepaymentPaymentSourceIds.Contains(line.SourceId.Value))
        {
            return true;
        }

        if (!TryResolveOwnerAccrualPropertyId(line, out var propertyId))
            return false;

        var invoiceSourceCode = ResolveOwnerReportIncomeInvoiceSourceKey(line, "Payment");
        if (string.IsNullOrWhiteSpace(invoiceSourceCode))
            return false;

        var contextKey = BuildOwnerAccrualInvoiceContextKey(propertyId, invoiceSourceCode, string.Empty);
        if (!invoiceAccountingPeriodByKey.TryGetValue(contextKey, out var invoiceAccountingPeriod))
            return false;

        return line.TransactionDate < invoiceAccountingPeriod;
    }
}
