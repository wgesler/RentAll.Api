using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetEscrowReportJournalEntryLinesAsync(
        EscrowReportJournalEntryDrillDownCriteria criteria)
    {
        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0 || !criteria.EndDate.HasValue)
            return [];

        var recapCriteria = BuildEscrowDrillDownRecapCriteria(criteria);
        var metric = (criteria.Metric ?? string.Empty).Trim().ToLowerInvariant();
        var needsRecapLines = EscrowDrillDownMetricNeedsRecapLines(metric);
        var bundle = await _journalEntryRepository.GetEscrowReportBundleDataAsync(recapCriteria, includeDrillDownLines: true);
        var recapLines = needsRecapLines
            ? await LoadEscrowDrillDownRecapLinesAsync(recapCriteria)
            : [];

        return metric switch
        {
            "arbalance" => FilterEscrowDrillDownLines(FilterEscrowDrillDownLinesByProperty(bundle.OwnerApLines, recapCriteria.PropertyId)),
            "notcollected" => BuildEscrowNotCollectedLines(recapLines),
            "prepaids" => BuildEscrowPrepaidLines(bundle, recapCriteria.PropertyId),
            "total" => BuildEscrowTotalDrillDownLines(bundle, recapLines, recapCriteria.PropertyId),
            "e2" => BuildEscrowE2DrillDownLines(bundle, recapLines, recapCriteria),
            "escrowbankbalance" => FilterEscrowDrillDownLines(bundle.EscrowBankLines, useAbsoluteAmount: true),
            "transfer" => BuildEscrowTransferDrillDownLines(bundle, recapLines, recapCriteria.PropertyId),
            _ => []
        };
    }

    private static bool EscrowDrillDownMetricNeedsRecapLines(string metric)
        => metric is "notcollected" or "total" or "e2" or "transfer";

    private static JournalEntryRecapGetCriteria BuildEscrowDrillDownRecapCriteria(
        EscrowReportJournalEntryDrillDownCriteria criteria)
    {
        return new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = null,
            EndDate = criteria.EndDate,
            IncludeUnposted = criteria.IncludeUnposted,
            IncludePaymentInvoiceContext = true
        };
    }

    private async Task<List<JournalEntryRecapLine>> LoadEscrowDrillDownRecapLinesAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        return FilterEscrowDrillDownRecapLines(lines, criteria.PropertyId);
    }

    private static List<JournalEntryRecapLine> FilterEscrowDrillDownRecapLines(
        IEnumerable<JournalEntryRecapLine> lines,
        Guid? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
            return lines.Where(line => line.Amount != 0).ToList();

        return lines
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value == propertyId.Value)
            .Where(line => line.Amount != 0)
            .ToList();
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> FilterEscrowDrillDownLinesByProperty(
        IEnumerable<OwnerStatementJournalEntryLine> lines,
        Guid? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
            return lines;

        return lines.Where(line => line.PropertyId == propertyId.Value);
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> FilterEscrowDrillDownLines(
        IEnumerable<OwnerStatementJournalEntryLine> lines,
        bool useAbsoluteAmount = false)
    {
        return lines
            .Where(row => row.Amount != 0)
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => useAbsoluteAmount ? Math.Abs(line.Amount) : line.Amount);
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowTotalDrillDownLines(
        EscrowReportBundleData bundle,
        List<JournalEntryRecapLine> recapLines,
        Guid? propertyId)
        => DistinctEscrowJournalEntryLines(
            FilterEscrowDrillDownLines(FilterEscrowDrillDownLinesByProperty(bundle.OwnerApLines, propertyId))
                .Concat(BuildEscrowNotCollectedLines(recapLines))
                .Concat(BuildEscrowPrepaidLines(bundle, propertyId)));

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowTransferDrillDownLines(
        EscrowReportBundleData bundle,
        List<JournalEntryRecapLine> recapLines,
        Guid? propertyId)
        => DistinctEscrowJournalEntryLines(
            FilterEscrowDrillDownLines(FilterEscrowDrillDownLinesByProperty(bundle.OwnerApLines, propertyId))
                .Concat(BuildEscrowNotCollectedLines(recapLines))
                .Concat(BuildEscrowPrepaidLines(bundle, propertyId))
                .Concat(FilterEscrowDrillDownLines(bundle.EscrowBankLines, useAbsoluteAmount: true)));

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowNotCollectedLines(IEnumerable<JournalEntryRecapLine> recapLines)
    {
        var lines = recapLines.ToList();
        var unpaidInvoiceKeys = lines
            .Where(line => line.PropertyId.HasValue)
            .GroupBy(line => $"{line.PropertyId!.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase)
            .Where(group => CalculateEscrowInvoiceUnpaidAmount(group) > 0.005m)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return lines
            .Where(line => line.PropertyId.HasValue
                && unpaidInvoiceKeys.Contains($"{line.PropertyId.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}"))
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .Select(line => BuildEscrowJournalEntryLine(line, "Actual", line.Amount))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);
    }

    private static decimal CalculateEscrowInvoiceUnpaidAmount(IEnumerable<JournalEntryRecapLine> invoiceLines)
    {
        var ownerRent = invoiceLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRent", StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Amount);
        var ownerPaid = invoiceLines
            .Where(line => string.Equals(line.RecapCategory, "OwnerRentActual", StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Amount);

        return CalculateUnpaidIncome(ownerRent, ownerPaid);
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowE2DrillDownLines(
        EscrowReportBundleData bundle,
        List<JournalEntryRecapLine> recapLines,
        JournalEntryRecapGetCriteria criteria)
    {
        if (criteria.PropertyId.HasValue && criteria.PropertyId.Value != Guid.Empty)
            return BuildEscrowE2DrillDownLinesForProperty(bundle, recapLines, criteria.PropertyId.Value);

        var allLines = new List<OwnerStatementJournalEntryLine>();
        foreach (var property in bundle.Properties ?? [])
        {
            if (property.PropertyId == Guid.Empty)
                continue;

            var propertyRecapLines = FilterEscrowDrillDownRecapLines(recapLines, property.PropertyId);
            allLines.AddRange(BuildEscrowE2DrillDownLinesForProperty(bundle, propertyRecapLines, property.PropertyId));
        }

        return DistinctEscrowJournalEntryLines(allLines);
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowE2DrillDownLinesForProperty(
        EscrowReportBundleData bundle,
        List<JournalEntryRecapLine> recapLines,
        Guid propertyId)
    {
        var ownerApLines = FilterEscrowDrillDownLinesByProperty(bundle.OwnerApLines, propertyId).ToList();
        var arBalance = ownerApLines.Sum(line => line.Amount);
        var prepaidDetails = FilterEscrowPrepaidDetailsByProperty(bundle.PrepaidPropertyBalances, propertyId);
        var prepaids = CalculateEscrowPrepaidAmount(prepaidDetails);
        var notCollected = recapLines
            .Where(line => line.PropertyId.HasValue)
            .GroupBy(line => $"{line.PropertyId!.Value:N}|{(line.SourceDocumentCode ?? string.Empty).Trim()}", StringComparer.OrdinalIgnoreCase)
            .Sum(group => CalculateEscrowInvoiceUnpaidAmount(group));
        var total = arBalance + prepaids - notCollected;
        if (total <= 0.005m)
            return [];

        return DistinctEscrowJournalEntryLines(
            FilterEscrowDrillDownLines(ownerApLines)
                .Concat(BuildEscrowNotCollectedLines(recapLines))
                .Concat(BuildEscrowPrepaidLines(bundle, propertyId)));
    }

    private static IEnumerable<EscrowPrepaidPropertyBalance> FilterEscrowPrepaidDetailsByProperty(
        IEnumerable<EscrowPrepaidPropertyBalance> prepaidDetails,
        Guid? propertyId)
    {
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
            return prepaidDetails ?? [];

        return (prepaidDetails ?? []).Where(detail => detail.PropertyId == propertyId.Value);
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> BuildEscrowPrepaidLines(
        EscrowReportBundleData bundle,
        Guid? propertyId)
    {
        return FilterEscrowDrillDownLines(
            FilterEscrowDrillDownLinesByProperty(bundle.PrepaidApplyLines, propertyId)
                .Where(line => line.Amount > 0.005m)
                .Select(line => new OwnerStatementJournalEntryLine
                {
                    JournalEntryLineId = line.JournalEntryLineId,
                    JournalEntryId = line.JournalEntryId,
                    JournalEntryCode = line.JournalEntryCode,
                    TransactionDate = line.TransactionDate,
                    OfficeId = line.OfficeId,
                    PropertyId = line.PropertyId,
                    PropertyCode = line.PropertyCode,
                    ChartOfAccountId = line.ChartOfAccountId,
                    AccountNo = line.AccountNo,
                    ChartOfAccountName = line.ChartOfAccountName,
                    Description = line.Description,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Category = "PrePaid",
                    Amount = line.Amount
                }));
    }

    private static IEnumerable<OwnerStatementJournalEntryLine> DistinctEscrowJournalEntryLines(
        IEnumerable<OwnerStatementJournalEntryLine> lines)
        => lines
            .GroupBy(line => line.JournalEntryLineId)
            .Select(group => group.First())
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount);

    private static OwnerStatementJournalEntryLine BuildEscrowJournalEntryLine(
        JournalEntryRecapLine line,
        string category,
        decimal amount)
    {
        return new OwnerStatementJournalEntryLine
        {
            JournalEntryLineId = line.JournalEntryLineId,
            JournalEntryId = line.JournalEntryId,
            JournalEntryCode = line.JournalEntryCode,
            TransactionDate = line.TransactionDate,
            OfficeId = line.OfficeId,
            PropertyId = line.PropertyId ?? Guid.Empty,
            PropertyCode = (line.PropertyCode ?? string.Empty).Trim(),
            ChartOfAccountId = line.ChartOfAccountId,
            AccountNo = line.AccountNo,
            ChartOfAccountName = line.ChartOfAccountName,
            Description = line.Description,
            Debit = line.Debit,
            Credit = line.Credit,
            Category = category,
            Amount = amount
        };
    }
}
