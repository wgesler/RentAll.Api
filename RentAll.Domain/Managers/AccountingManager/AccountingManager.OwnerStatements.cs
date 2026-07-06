using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    public async Task<IEnumerable<OwnerStatementSummary>> GetOwnerStatementsAsync(OwnerStatementGetCriteria criteria)
    {
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return Enumerable.Empty<OwnerStatementSummary>();

        var summaries = new List<OwnerStatementSummary>();
        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
            var officeCriteria = new OwnerStatementGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                PropertyId = criteria.PropertyId,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ExpectedAccountId = ownerAccountsPayableAccountId,
                ActualAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
                PrePaidAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
                ExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice)
            };
            var currentOfficeSummaries = (await _accountingRepository.GetOwnerStatementsAsync(officeCriteria)).ToList();
            var previousMonthOwnerApBalanceByKey = await GetPreviousMonthOwnerApBalanceByKeyAsync(officeCriteria, ownerAccountsPayableAccountId);

            foreach (var summary in currentOfficeSummaries)
            {
                summary.StartingBalance = previousMonthOwnerApBalanceByKey.TryGetValue(BuildOwnerStatementSummaryKey(summary), out var ownerApBalance)
                    ? ownerApBalance
                    : 0m;
                ApplyOwnerStatementSummaryCalculations(summary);
            }

            summaries.AddRange(currentOfficeSummaries);
        }

        if (summaries.Count == 0)
            return summaries;

        return summaries
            .OrderBy(summary => summary.OfficeName)
            .ThenBy(summary => summary.PropertyCode)
            .ToList();
    }

    public async Task<OwnerStatementSearchResult> GetOwnerStatementSearchResultAsync(OwnerStatementGetCriteria criteria)
    {
        var summaries = (await GetOwnerStatementsAsync(criteria)).ToList();
        var propertyActivityLines = await GetOwnerStatementPropertyActivityLinesByCriteriaAsync(criteria);

        return new OwnerStatementSearchResult
        {
            Summaries = summaries,
            PropertyActivityLines = propertyActivityLines
        };
    }

    public async Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerStatementJournalEntryLinesAsync(OwnerStatementJournalEntryLineGetCriteria criteria)
    {
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return Enumerable.Empty<OwnerStatementJournalEntryLine>();

        var lines = new List<OwnerStatementJournalEntryLine>();
        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var officeCriteria = new OwnerStatementJournalEntryLineGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                OwnerId = criteria.OwnerId,
                PropertyId = criteria.PropertyId,
                Metric = criteria.Metric,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ExpectedAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice),
                ActualAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
                PrePaidAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
                ExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice)
            };
            lines.AddRange(await _accountingRepository.GetOwnerStatementJournalEntryLinesAsync(officeCriteria));
        }

        return lines
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .ThenByDescending(line => line.Amount)
            .ToList();
    }

    public async Task<IEnumerable<OwnerStatementPropertyActivityLine>> GetOwnerStatementPropertyActivityLinesAsync(OwnerStatementPropertyActivityGetCriteria criteria)
    {
        if (criteria.PropertyId == Guid.Empty)
            return Enumerable.Empty<OwnerStatementPropertyActivityLine>();

        return await GetOwnerStatementPropertyActivityLinesByCriteriaAsync(new OwnerStatementGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });
    }

    private async Task<List<OwnerStatementPropertyActivityLine>> GetOwnerStatementPropertyActivityLinesByCriteriaAsync(OwnerStatementGetCriteria criteria)
    {
        var propertyActivityLines = new List<OwnerStatementPropertyActivityLine>();
        var officeIds = ParseOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return propertyActivityLines;

        foreach (var officeId in officeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(criteria.OrganizationId, officeId);
            var officeCriteria = new OwnerStatementGetCriteria
            {
                OrganizationId = criteria.OrganizationId,
                OfficeIds = officeId.ToString(),
                PropertyId = criteria.PropertyId,
                StartDate = criteria.StartDate,
                EndDate = criteria.EndDate,
                ExpectedAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice),
                ActualAccountId = GetDefaultUndepositedFunds(chartOfAccounts, officeId, accountingOffice),
                PrePaidAccountId = GetDefaultPrePayment(chartOfAccounts, officeId, accountingOffice),
                ExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice)
            };
            propertyActivityLines.AddRange(await _accountingRepository.GetOwnerStatementPropertyActivityLinesByCriteriaAsync(officeCriteria));
        }

        return propertyActivityLines
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId)
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => line.ActivityType)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    public async Task<JournalEntry?> CreateOwnerStatementStartingBalanceJournalEntryAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId, DateOnly transactionDate, decimal amount, Guid currentUser)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return null;
        if (officeId <= 0 || ownerId == Guid.Empty || propertyId == Guid.Empty || transactionDate == default || amount == 0)
            throw new Exception("Office, owner, property, transaction date, and non-zero amount are required to create owner starting balance.");

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
        var ownerExpenseAccountId = GetDefaultOwnerExpense(chartOfAccounts, officeId, accountingOffice);
        var memo = $"{OwnerStartingBalanceMemoPrefix} {transactionDate:MM/yyyy}";
        var startingBalance = Math.Abs(amount);
        var isPositive = amount > 0;
        var journalEntry = new JournalEntry
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            TransactionDate = transactionDate,
            SourceTypeId = (int)SourceType.Adjustment,
            Memo = memo,
            JournalEntryLines = new List<JournalEntryLine>
            {
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerExpenseAccountId,
                    PropertyId = propertyId,
                    ContactId = ownerId,
                    Debit = isPositive ? startingBalance : 0,
                    Credit = isPositive ? 0 : startingBalance,
                    Memo = memo,
                    CreatedBy = currentUser
                },
                new JournalEntryLine
                {
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = propertyId,
                    ContactId = ownerId,
                    Debit = isPositive ? 0 : startingBalance,
                    Credit = isPositive ? startingBalance : 0,
                    Memo = memo,
                    CreatedBy = currentUser
                }
            },
            CreatedBy = currentUser
        };
        var createdJournalEntry = await CreateJournalEntryAsync(journalEntry);
        if (createdJournalEntry == null)
            return null;

        return await PostJournalEntryAsync(createdJournalEntry.JournalEntryId, organizationId, currentUser);
    }

    public async Task<OwnerStatementStartingBalanceEntry?> GetOwnerStatementStartingBalanceAsync(Guid organizationId, int officeId, Guid ownerId, Guid propertyId)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return null;
        if (officeId <= 0 || ownerId == Guid.Empty || propertyId == Guid.Empty)
            return null;

        var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
        var ownerAccountsPayableAccountId = GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice);
        var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            SourceTypeId = (int)SourceType.Adjustment,
            ChartOfAccountId = ownerAccountsPayableAccountId,
            PropertyId = propertyId,
            ContactId = ownerId,
            IncludeVoided = false,
            IncludeUnposted = true
        });
        var current = lines
            .Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .FirstOrDefault();
        if (current == null)
            return null;

        return new OwnerStatementStartingBalanceEntry
        {
            JournalEntryId = current.JournalEntryId,
            OfficeId = current.OfficeId,
            OwnerId = current.ContactId ?? Guid.Empty,
            PropertyId = current.PropertyId ?? Guid.Empty,
            TransactionDate = current.TransactionDate,
            Amount = current.Credit - current.Debit,
            Memo = (current.JournalEntryMemo ?? current.Memo ?? string.Empty).Trim(),
            IsPosted = current.IsPosted
        };
    }

    private static List<int> ParseOfficeIds(string officeIdsCsv)
    {
        return officeIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();
    }

    private async Task<Dictionary<string, decimal>> GetPreviousMonthOwnerApBalanceByKeyAsync(OwnerStatementGetCriteria officeCriteria, int ownerAccountsPayableAccountId)
    {
        var priorMonthClose = ResolvePriorMonthCloseDate(officeCriteria.StartDate, officeCriteria.EndDate);
        if (!priorMonthClose.HasValue)
            return new Dictionary<string, decimal>();

        var priorMonthOwnerApLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = officeCriteria.OrganizationId,
            OfficeIds = officeCriteria.OfficeIds,
            ChartOfAccountId = ownerAccountsPayableAccountId,
            PropertyId = officeCriteria.PropertyId,
            StartDate = null,
            EndDate = priorMonthClose,
            IncludeVoided = false,
            IncludeUnposted = true
        });

        return priorMonthOwnerApLines
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
            .GroupBy(line => BuildOwnerStatementSummaryKey(line.OfficeId, line.PropertyId!.Value, line.ContactId))
            .ToDictionary(
                group => group.Key,
                CalculateOwnerApBalanceFromInitialStartingBalanceWindow);
    }

    private static decimal CalculateOwnerApBalanceFromInitialStartingBalanceWindow(IGrouping<string, JournalEntryLineSearchResult> group)
    {
        var orderedLines = group
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();
        var initialStartingBalanceLine = orderedLines.FirstOrDefault(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo));
        if (initialStartingBalanceLine == null)
            return orderedLines.Sum(line => line.Credit - line.Debit);

        return orderedLines
            .Where(line => line.TransactionDate >= initialStartingBalanceLine.TransactionDate)
            .Sum(line => line.Credit - line.Debit);
    }

    private static string BuildOwnerStatementSummaryKey(OwnerStatementSummary summary)
        => BuildOwnerStatementSummaryKey(summary.OfficeId, summary.PropertyId, summary.OwnerId);

    private static string BuildOwnerStatementSummaryKey(int officeId, Guid propertyId, Guid? ownerId)
        => $"{officeId}:{propertyId:D}:{ownerId?.ToString("D") ?? "none"}";

    private static DateOnly? ResolvePriorMonthCloseDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue)
            return startDate.Value.AddDays(-1);
        if (endDate.HasValue)
        {
            var firstDayOfMonth = new DateOnly(endDate.Value.Year, endDate.Value.Month, 1);
            return firstDayOfMonth.AddDays(-1);
        }

        return null;
    }

    private static void ApplyOwnerStatementSummaryCalculations(OwnerStatementSummary summary)
    {
        // Outstanding and PaidIncome are computed in Accounting.OwnerStatement_GetByCriteria.
        summary.Balance = summary.Income - summary.Expenses;
        summary.WorkingCapitalBalanceDue = summary.Balance;
        summary.OwnerPayment = summary.StartingBalance + summary.Income - summary.Expenses - summary.WorkingCapital;
        summary.EndingBalance = summary.StartingBalance + summary.Income - summary.Expenses - summary.OwnerPayment;
    }

    private static bool IsOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo)
    {
        var summaryMemo = (journalMemo ?? string.Empty).Trim();
        var detailMemo = (lineMemo ?? string.Empty).Trim();
        return summaryMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase)
            || detailMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase);
    }

}
