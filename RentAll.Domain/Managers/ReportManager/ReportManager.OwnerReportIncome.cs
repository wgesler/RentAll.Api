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
        var invoiceContextByKey = BuildOwnerAccrualInvoiceContextByKey(invoiceContextLines);
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
}
