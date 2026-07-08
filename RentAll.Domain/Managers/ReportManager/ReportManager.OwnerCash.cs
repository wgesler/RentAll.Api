using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    public async Task<OwnerCashReport> GetOwnerCashReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        criteria.IncludePaymentInvoiceContext = true;
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        var activitySourceLines = lines.Where(line => line.IsInDateRange).ToList();
        var officeIds = ParseReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return new OwnerCashReport();

        var properties = await LoadOwnerCashPropertyReportDataAsync(criteria);
        var startingBalanceByKey = await GetOwnerCashStartingBalanceByKeyAsync(criteria, officeIds);
        var propertyActivityLines = BuildOwnerReportPropertyActivityLines(
            activitySourceLines,
            lines,
            OwnerReportActivityMode.Cash);
        var activityLinesByProperty = BuildOwnerReportPropertyActivityLinesByKey(propertyActivityLines);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = BuildOwnerCashPropertyKey(property.OfficeId, property.PropertyId);
                var startingBalance = ResolveOwnerCashStartingBalance(
                    startingBalanceByKey,
                    property.OfficeId,
                    property.PropertyId,
                    property.PrimaryOwnerId);
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];

                var receivedIncome = activityLines.Sum(line => line.ReceivedIncome);
                var ownerExpenses = activityLines.Sum(line => line.Expenses);
                var ownerPayment = CalculateOwnerCashOwnerPayment(
                    startingBalance,
                    receivedIncome,
                    ownerExpenses,
                    property.WorkingCapitalBalance);
                var endingBalance = CalculateOwnerCashEndingBalance(
                    startingBalance,
                    receivedIncome,
                    ownerExpenses,
                    ownerPayment);

                return new OwnerCashReportRow
                {
                    PropertyId = property.PropertyId,
                    OfficeId = property.OfficeId,
                    OfficeName = property.OfficeName,
                    OwnerId = property.PrimaryOwnerId,
                    PropertyCode = property.PropertyCode,
                    CompanyName = property.CompanyName,
                    OwnerNames = property.OwnerNames,
                    OwnerNameLine = property.OwnerNameLine,
                    StartingBalance = startingBalance,
                    ReceivedIncome = receivedIncome,
                    OwnerExpenses = ownerExpenses,
                    OwnerPayment = ownerPayment,
                    EndingBalance = endingBalance,
                    WorkingCapital = property.WorkingCapitalBalance
                };
            })
            .OrderBy(row => row.OfficeName)
            .ThenBy(row => row.PropertyCode)
            .ToList();

        return new OwnerCashReport
        {
            Rows = rows,
            PropertyActivityLines = propertyActivityLines
        };
    }

    private static Dictionary<string, List<OwnerStatementPropertyActivityLine>> BuildOwnerReportPropertyActivityLinesByKey(
        IEnumerable<OwnerStatementPropertyActivityLine> lines)
    {
        return lines
            .GroupBy(line => BuildOwnerCashPropertyKey(line.OfficeId, line.PropertyId))
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<PropertyReportData>> LoadOwnerCashPropertyReportDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        var properties = (await _propertyRepository.GetPropertyReportDataAsync(
            criteria.OrganizationId,
            criteria.OfficeIds,
            criteria.PropertyId)).ToList();

        return properties
            .Where(property => property.PropertyLeaseType == PropertyLeaseType.PropertyManagement)
            .Where(property => property.PrimaryOwnerId.HasValue && property.PrimaryOwnerId.Value != Guid.Empty)
            .OrderBy(property => property.OfficeName)
            .ThenBy(property => property.PropertyCode)
            .ToList();
    }

    private async Task<Dictionary<string, decimal>> GetOwnerCashStartingBalanceByKeyAsync(
        JournalEntryRecapGetCriteria criteria,
        IReadOnlyList<int> officeIds)
    {
        var startingBalanceByKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var priorMonthClose = ResolveOwnerCashPriorMonthCloseDate(criteria.StartDate, criteria.EndDate);
        var periodStart = ResolveOwnerReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        if (!priorMonthClose.HasValue && !periodStart.HasValue)
            return startingBalanceByKey;

        foreach (var officeId in officeIds)
        {
            var chartOfAccounts = (await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(criteria.OrganizationId, officeId)).ToList();
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = ResolveOwnerAccountsPayableAccountId(chartOfAccounts, officeId, accountingOffice);

            if (priorMonthClose.HasValue)
            {
                var priorMonthOwnerApLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
                {
                    OrganizationId = criteria.OrganizationId,
                    OfficeIds = officeId.ToString(),
                    ChartOfAccountId = ownerAccountsPayableAccountId,
                    PropertyId = criteria.PropertyId,
                    StartDate = null,
                    EndDate = priorMonthClose,
                    IncludeVoided = false,
                    IncludeUnposted = true
                });

                foreach (var group in priorMonthOwnerApLines
                             .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
                             .GroupBy(line => BuildOwnerCashStartingBalanceKey(
                                 line.OfficeId,
                                 line.PropertyId!.Value,
                                 line.ContactId)))
                {
                    startingBalanceByKey[group.Key] = CalculateOwnerApBalanceFromInitialStartingBalanceWindow(group);
                }
            }

            if (periodStart.HasValue)
            {
                var reportEnd = ResolveOwnerReportPeriodEndDate(criteria.StartDate, criteria.EndDate);
                if (reportEnd.HasValue)
                {
                    await ApplyInReportRangeStartingBalanceSupplementAsync(
                        criteria,
                        officeId,
                        ownerAccountsPayableAccountId,
                        periodStart.Value,
                        reportEnd.Value,
                        startingBalanceByKey);
                }
            }
        }

        return startingBalanceByKey;
    }

    private async Task ApplyInReportRangeStartingBalanceSupplementAsync(
        JournalEntryRecapGetCriteria criteria,
        int officeId,
        int ownerAccountsPayableAccountId,
        DateOnly periodStart,
        DateOnly reportEnd,
        Dictionary<string, decimal> startingBalanceByKey)
    {
        var inRangeOwnerApLines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = officeId.ToString(),
            ChartOfAccountId = ownerAccountsPayableAccountId,
            PropertyId = criteria.PropertyId,
            StartDate = periodStart,
            EndDate = reportEnd,
            IncludeVoided = false,
            IncludeUnposted = true
        });

        foreach (var propertyGroup in inRangeOwnerApLines
                     .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
                     .Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo))
                     .GroupBy(line => BuildOwnerCashStartingBalanceKey(
                         line.OfficeId,
                         line.PropertyId!.Value,
                         line.ContactId)))
        {
            if (startingBalanceByKey.TryGetValue(propertyGroup.Key, out var existingBalance) && existingBalance != 0)
                continue;

            var earliestStartingBalanceEntry = propertyGroup
                .GroupBy(line => line.JournalEntryId)
                .Select(journalEntryGroup =>
                {
                    var firstLine = journalEntryGroup.First();
                    return new
                    {
                        firstLine.TransactionDate,
                        firstLine.JournalEntryCode,
                        NetBalance = journalEntryGroup.Sum(line => line.Credit - line.Debit)
                    };
                })
                .Where(entry => entry.NetBalance != 0)
                .OrderBy(entry => entry.TransactionDate)
                .ThenBy(entry => entry.JournalEntryCode)
                .FirstOrDefault();

            if (earliestStartingBalanceEntry == null)
                continue;

            startingBalanceByKey[propertyGroup.Key] = earliestStartingBalanceEntry.NetBalance;
        }
    }

    private static decimal ResolveOwnerCashStartingBalance(
        IReadOnlyDictionary<string, decimal> startingBalanceByKey,
        int officeId,
        Guid propertyId,
        Guid? ownerId)
    {
        var ownerKey = BuildOwnerCashStartingBalanceKey(officeId, propertyId, ownerId);
        if (startingBalanceByKey.TryGetValue(ownerKey, out var balance))
            return balance;

        var noneKey = BuildOwnerCashStartingBalanceKey(officeId, propertyId, null);
        if (startingBalanceByKey.TryGetValue(noneKey, out balance))
            return balance;

        var propertyPrefix = $"{officeId}:{propertyId:D}:";
        return startingBalanceByKey
            .Where(entry => entry.Key.StartsWith(propertyPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Value)
            .FirstOrDefault();
    }

    private static decimal CalculateOwnerCashOwnerPayment(
        decimal startingBalance,
        decimal receivedIncome,
        decimal ownerExpenses,
        decimal workingCapitalBalance)
    {
        var ownerPayment = startingBalance + receivedIncome - ownerExpenses - workingCapitalBalance;
        return ownerPayment < 0 ? 0 : ownerPayment;
    }

    private static decimal CalculateOwnerCashEndingBalance(
        decimal startingBalance,
        decimal receivedIncome,
        decimal ownerExpenses,
        decimal ownerPayment)
    {
        var endingBalance = startingBalance + receivedIncome - ownerExpenses - ownerPayment;
        return endingBalance < 0 ? 0 : endingBalance;
    }

    private static string StripOwnerMemoPrefixForCashDisplay(string memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        if (trimmed.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase))
            return trimmed["Owner:".Length..].TrimStart();

        return trimmed;
    }

    private static int ResolveOwnerAccountsPayableAccountId(
        IReadOnlyList<ChartOfAccount> chartOfAccounts,
        int officeId,
        AccountingOffice? accountingOffice)
    {
        if (accountingOffice?.DefaultOwnActPayableAccountId is > 0)
            return accountingOffice.DefaultOwnActPayableAccountId.Value;

        var account = chartOfAccounts
            .Where(account => account.OfficeId == officeId && account.AccountType == AccountType.AccountsPayable)
            .Where(account => account.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase))
            .OrderBy(account => account.AccountId)
            .FirstOrDefault()
            ?? chartOfAccounts
                .Where(account => account.OfficeId == officeId && account.AccountType == AccountType.AccountsPayable)
                .OrderBy(account => account.AccountId)
                .FirstOrDefault();

        if (account == null)
            throw new Exception($"No Owner Accounts Payable chart of account is configured for office {officeId}.");

        return account.AccountId;
    }

    private static decimal CalculateOwnerApBalanceFromInitialStartingBalanceWindow(
        IGrouping<string, JournalEntryLineSearchResult> group)
    {
        var orderedLines = group
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();
        var initialStartingBalanceLine = orderedLines.FirstOrDefault(line =>
            IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo));
        if (initialStartingBalanceLine == null)
            return orderedLines.Sum(line => line.Credit - line.Debit);

        return orderedLines
            .Where(line => line.TransactionDate >= initialStartingBalanceLine.TransactionDate)
            .Sum(line => line.Credit - line.Debit);
    }

    private static List<int> ParseReportOfficeIds(string officeIdsCsv)
    {
        return officeIdsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : 0)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();
    }

    private static string BuildOwnerCashPropertyKey(int officeId, Guid propertyId)
        => $"{officeId}:{propertyId:D}";

    private static string BuildOwnerCashStartingBalanceKey(int officeId, Guid propertyId, Guid? ownerId)
        => $"{officeId}:{propertyId:D}:{ownerId?.ToString("D") ?? "none"}";

    private static DateOnly? ResolveOwnerCashPriorMonthCloseDate(DateOnly? startDate, DateOnly? endDate)
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

    private static DateOnly? ResolveOwnerReportPeriodStartDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue)
            return startDate.Value;

        if (endDate.HasValue)
            return new DateOnly(endDate.Value.Year, endDate.Value.Month, 1);

        return null;
    }

    private static DateOnly? ResolveOwnerReportPeriodEndDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (endDate.HasValue)
            return endDate.Value;

        if (startDate.HasValue)
            return startDate.Value;

        return null;
    }

    private static bool IsOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo)
    {
        var summaryMemo = (journalMemo ?? string.Empty).Trim();
        var detailMemo = (lineMemo ?? string.Empty).Trim();
        return summaryMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase)
            || detailMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
