using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    public async Task<OwnerCashReport> GetOwnerCashReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var lines = (await _journalEntryRepository.GetJournalEntryRecapLinesAsync(criteria)).ToList();
        var recapRows = BuildRecapReportRows(lines);
        var officeIds = ParseReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
            return new OwnerCashReport();

        var properties = await LoadOwnerCashPropertyReportDataAsync(criteria);
        var startingBalanceByKey = await GetOwnerCashStartingBalanceByKeyAsync(criteria, officeIds);
        var propertyActivityLines = BuildOwnerCashPropertyActivityLines(recapRows);
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
                    OwnerName = (property.OwnerNames ?? string.Empty).Trim(),
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

    private static bool HasOwnerCashReportRecapActivity(RecapReportRow row) =>
        row.OwnerRentValue != 0
        || row.OwnerExpenseValue != 0
        || row.OwnerPaymentValue != 0;

    private static bool HasOwnerAccrualReportRecapActivity(RecapReportRow row) =>
        row.ExpectedIncomeValue != 0
        || HasOwnerCashReportRecapActivity(row);

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
        if (!priorMonthClose.HasValue)
            return startingBalanceByKey;

        foreach (var officeId in officeIds)
        {
            var chartOfAccounts = (await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(criteria.OrganizationId, officeId)).ToList();
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(criteria.OrganizationId, officeId);
            var ownerAccountsPayableAccountId = ResolveOwnerAccountsPayableAccountId(chartOfAccounts, officeId, accountingOffice);

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

        return startingBalanceByKey;
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

    private static List<OwnerStatementPropertyActivityLine> BuildOwnerCashPropertyActivityLines(IEnumerable<RecapReportRow> recapRows)
    {
        return recapRows
            .Where(row => row.PropertyId.HasValue && row.PropertyId.Value != Guid.Empty)
            .Where(row => HasOwnerCashReportRecapActivity(row))
            .Select(row => new OwnerStatementPropertyActivityLine
            {
                PropertyId = row.PropertyId!.Value,
                OfficeId = row.OfficeId,
                ActivityId = ResolveOwnerCashActivityJournalEntryLineId(row),
                SourceId = row.SourceId,
                JournalEntryLineId = ResolveOwnerCashActivityJournalEntryLineId(row),
                ActivityType = row.ActivityType,
                ActivityDate = ParseOwnerCashActivityDate(row.TransactionDate),
                DocumentCode = ResolveOwnerCashActivityDocumentCode(row),
                Description = ResolveOwnerCashActivityDescription(row),
                ExpectedIncome = 0,
                ReceivedIncome = row.OwnerRentValue,
                Expenses = row.OwnerExpenseValue,
                OwnerPayment = row.OwnerPaymentValue
            })
            .OrderBy(line => line.OfficeId)
            .ThenBy(line => line.PropertyId)
            .ThenBy(line => line.ActivityDate)
            .ThenBy(line => line.DocumentCode)
            .ToList();
    }

    private static Guid? ResolveOwnerCashActivityJournalEntryLineId(RecapReportRow row)
    {
        if (row.OwnerRentValue != 0 && row.OwnerRentJournalEntryLineId.HasValue)
            return row.OwnerRentJournalEntryLineId;

        if (row.OwnerExpenseValue != 0 && row.OwnerExpenseJournalEntryLineId.HasValue)
            return row.OwnerExpenseJournalEntryLineId;

        if (row.OwnerPaymentValue != 0 && row.OwnerPaymentJournalEntryLineId.HasValue)
            return row.OwnerPaymentJournalEntryLineId;

        return row.JournalEntryLineId;
    }

    private static string ResolveOwnerCashActivityDocumentCode(RecapReportRow row)
    {
        if (row.OwnerRentValue != 0)
        {
            var ownerRentJournalEntryCode = (row.OwnerRentJournalEntryCode ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerRentJournalEntryCode))
                return ownerRentJournalEntryCode;
        }

        if (row.OwnerExpenseValue != 0)
        {
            var ownerExpenseJournalEntryCode = (row.OwnerExpenseJournalEntryCode ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerExpenseJournalEntryCode))
                return ownerExpenseJournalEntryCode;
        }

        if (row.OwnerPaymentValue != 0)
        {
            var ownerPaymentJournalEntryCode = (row.OwnerPaymentJournalEntryCode ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerPaymentJournalEntryCode))
                return ownerPaymentJournalEntryCode;
        }

        return (row.JournalEntryCode ?? string.Empty).Trim();
    }

    private static string ResolveOwnerCashActivityDescription(RecapReportRow row)
    {
        var memo = SelectOwnerCashActivityMemo(row);
        if (!string.IsNullOrWhiteSpace(memo))
            return StripOwnerMemoPrefixForCashDisplay(memo);

        return ResolveOwnerCashActivityDocumentCode(row);
    }

    private static string SelectOwnerCashActivityMemo(RecapReportRow row)
    {
        if (row.OwnerRentValue != 0)
        {
            var ownerRentMemo = (row.OwnerRentMemo ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerRentMemo))
                return ownerRentMemo;
        }

        if (row.OwnerExpenseValue != 0)
        {
            var ownerExpenseMemo = (row.OwnerExpenseMemo ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerExpenseMemo))
                return ownerExpenseMemo;
        }

        if (row.OwnerPaymentValue != 0)
        {
            var ownerPaymentMemo = (row.OwnerPaymentMemo ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(ownerPaymentMemo))
                return ownerPaymentMemo;
        }

        return string.Empty;
    }

    private static string StripOwnerMemoPrefixForCashDisplay(string memo)
    {
        var trimmed = (memo ?? string.Empty).Trim();
        if (trimmed.StartsWith("Owner:", StringComparison.OrdinalIgnoreCase))
            return trimmed["Owner:".Length..].TrimStart();

        return trimmed;
    }

    private static DateOnly ParseOwnerCashActivityDate(string transactionDate)
    {
        if (DateOnly.TryParse(transactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        return default;
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

    private static bool IsOwnerStartingBalanceMemo(string? journalMemo, string? lineMemo)
    {
        var summaryMemo = (journalMemo ?? string.Empty).Trim();
        var detailMemo = (lineMemo ?? string.Empty).Trim();
        return summaryMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase)
            || detailMemo.StartsWith(OwnerStartingBalanceMemoPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
