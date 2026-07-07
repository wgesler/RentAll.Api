using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerReportJournalEntryLinesAsync(
        OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        var officeIds = ParseReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0 || criteria.OwnerId == Guid.Empty)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var recapCriteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        };

        var properties = await LoadOwnerCashPropertyReportDataAsync(recapCriteria);
        var propertyIdsForOwner = properties
            .Where(property => property.PrimaryOwnerId == criteria.OwnerId)
            .Where(property => !criteria.PropertyId.HasValue || property.PropertyId == criteria.PropertyId.Value)
            .Select(property => property.PropertyId)
            .ToHashSet();

        if (propertyIdsForOwner.Count == 0)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var recapLines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(recapCriteria))
            .Where(line => line.PropertyId.HasValue && propertyIdsForOwner.Contains(line.PropertyId.Value))
            .Where(line => line.Amount != 0)
            .ToList();

        var metric = (criteria.Metric ?? string.Empty).Trim().ToLowerInvariant();
        return recapLines
            .Where(line => MatchesOwnerReportDrillDownMetric(line, metric))
            .Select(line => MapRecapLineToOwnerReportJournalEntryLine(line, metric))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount)
            .ToList();
    }

    private static bool MatchesOwnerReportDrillDownMetric(JournalEntryRecapLine line, string metric)
    {
        var category = (line.RecapCategory ?? string.Empty).Trim();

        return metric switch
        {
            "expected" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase),
            "prepaid" => string.Equals(category, "PrePayment", StringComparison.OrdinalIgnoreCase),
            "paidincome" => string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase),
            "outstanding" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase),
            "income" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase),
            "expenses" => string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase),
            "balance" => string.Equals(category, "OwnerRent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static OwnerStatementJournalEntryLine MapRecapLineToOwnerReportJournalEntryLine(
        JournalEntryRecapLine line,
        string metric)
    {
        var category = MapRecapCategoryToDrillDownCategory(line.RecapCategory);
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
            Amount = CalculateOwnerReportDrillDownAmount(line, metric, category)
        };
    }

    private static string MapRecapCategoryToDrillDownCategory(string? recapCategory)
    {
        return (recapCategory ?? string.Empty).Trim() switch
        {
            "ExpectedIncome" => "Expected",
            "PrePayment" => "PrePaid",
            "Payment" => "PaidIncome",
            "OwnerRent" => "Actual",
            "OwnerPayment" => "OwnerPayment",
            "Expense" => "Expense",
            _ => (recapCategory ?? string.Empty).Trim()
        };
    }

    private static decimal CalculateOwnerReportDrillDownAmount(
        JournalEntryRecapLine line,
        string metric,
        string category)
    {
        var amount = line.Amount;
        if (metric == "outstanding" && string.Equals(category, "OwnerPayment", StringComparison.OrdinalIgnoreCase))
            return -amount;

        if (metric == "balance" && string.Equals(category, "Expense", StringComparison.OrdinalIgnoreCase))
            return -amount;

        return amount;
    }
}
