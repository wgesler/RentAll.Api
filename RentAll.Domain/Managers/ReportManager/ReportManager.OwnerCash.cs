using System.Globalization;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private const string OwnerStartingBalanceMemoPrefix = "Owner: Starting Balance:";

    private sealed class OwnerCashStartingBalanceSnapshot
    {
        public decimal LedgerBalance { get; set; }
        public decimal OpeningBalanceJeAmount { get; set; }
        public DateOnly? OpeningBalanceTransactionDate { get; set; }
    }

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
        var priorPeriodUnpaidByPropertyKey = BuildOwnerCashPriorPeriodUnpaidByPropertyKey(
            lines,
            criteria,
            startingBalanceByKey);

        var rows = properties
            .Select(property =>
            {
                var propertyKey = BuildOwnerCashPropertyKey(property.OfficeId, property.PropertyId);
                var startingBalanceSnapshot = ResolveOwnerCashStartingBalanceSnapshot(
                    startingBalanceByKey,
                    property.OfficeId,
                    property.PropertyId,
                    property.PrimaryOwnerId);
                priorPeriodUnpaidByPropertyKey.TryGetValue(propertyKey, out var priorPeriodUnpaidIncome);
                var cancellableUnpaidIncome = Math.Min(
                    priorPeriodUnpaidIncome,
                    Math.Max(0m, startingBalanceSnapshot.LedgerBalance - startingBalanceSnapshot.OpeningBalanceJeAmount));
                var startingBalance = startingBalanceSnapshot.LedgerBalance - cancellableUnpaidIncome;
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

    private static Dictionary<string, decimal> BuildOwnerCashPriorPeriodUnpaidByPropertyKey(
        IReadOnlyList<JournalEntryRecapLine> lines,
        JournalEntryRecapGetCriteria criteria,
        IReadOnlyDictionary<string, OwnerCashStartingBalanceSnapshot> startingBalanceByKey)
    {
        var periodStart = ResolveOwnerReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        if (!periodStart.HasValue)
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var priorPeriodUnpaidByPropertyKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var propertyGroup in (lines ?? [])
                     .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
                     .Where(line => line.TransactionDate < periodStart.Value)
                     .GroupBy(line => BuildOwnerCashPropertyKey(line.OfficeId, line.PropertyId!.Value)))
        {
            var propertyKey = propertyGroup.Key;
            var openingBalanceTransactionDate = ResolveOwnerCashOpeningBalanceTransactionDate(
                startingBalanceByKey,
                propertyKey);

            var priorPeriodSourceLines = propertyGroup.AsEnumerable();
            if (openingBalanceTransactionDate.HasValue)
            {
                priorPeriodSourceLines = priorPeriodSourceLines
                    .Where(line => line.TransactionDate >= openingBalanceTransactionDate.Value);
            }

            var priorPeriodLines = priorPeriodSourceLines.ToList();
            if (priorPeriodLines.Count == 0)
                continue;

            var priorPeriodActivityLines = BuildOwnerReportPropertyActivityLines(
                priorPeriodLines,
                lines,
                OwnerReportActivityMode.Accrual);
            var invoicedIncome = priorPeriodActivityLines.Sum(line => line.ExpectedIncome);
            var paidIncome = priorPeriodActivityLines.Sum(line => line.ReceivedIncome);
            priorPeriodUnpaidByPropertyKey[propertyKey] = Math.Max(0m, invoicedIncome - paidIncome);
        }

        return priorPeriodUnpaidByPropertyKey;
    }

    private static DateOnly? ResolveOwnerCashOpeningBalanceTransactionDate(
        IReadOnlyDictionary<string, OwnerCashStartingBalanceSnapshot> startingBalanceByKey,
        string propertyKey)
    {
        if (startingBalanceByKey.TryGetValue(propertyKey, out var snapshot))
            return snapshot.OpeningBalanceTransactionDate;

        return null;
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

    private async Task<Dictionary<string, OwnerCashStartingBalanceSnapshot>> GetOwnerCashStartingBalanceByKeyAsync(
        JournalEntryRecapGetCriteria criteria,
        IReadOnlyList<int> officeIds)
    {
        var startingBalanceByKey = new Dictionary<string, OwnerCashStartingBalanceSnapshot>(StringComparer.OrdinalIgnoreCase);
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
                             .GroupBy(line => BuildOwnerCashPropertyKey(
                                 line.OfficeId,
                                 line.PropertyId!.Value)))
                {
                    startingBalanceByKey[group.Key] = CalculateOwnerApStartingBalanceSnapshot(group);
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
        Dictionary<string, OwnerCashStartingBalanceSnapshot> startingBalanceByKey)
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
                     .GroupBy(line => BuildOwnerCashPropertyKey(
                         line.OfficeId,
                         line.PropertyId!.Value)))
        {
            if (startingBalanceByKey.TryGetValue(propertyGroup.Key, out var existingSnapshot)
                && existingSnapshot.LedgerBalance != 0)
            {
                continue;
            }

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

            startingBalanceByKey[propertyGroup.Key] = new OwnerCashStartingBalanceSnapshot
            {
                LedgerBalance = earliestStartingBalanceEntry.NetBalance,
                OpeningBalanceJeAmount = earliestStartingBalanceEntry.NetBalance,
                OpeningBalanceTransactionDate = earliestStartingBalanceEntry.TransactionDate
            };
        }
    }

    private static OwnerCashStartingBalanceSnapshot ResolveOwnerCashStartingBalanceSnapshot(
        IReadOnlyDictionary<string, OwnerCashStartingBalanceSnapshot> startingBalanceByKey,
        int officeId,
        Guid propertyId,
        Guid? ownerId)
    {
        var propertyKey = BuildOwnerCashPropertyKey(officeId, propertyId);
        if (startingBalanceByKey.TryGetValue(propertyKey, out var snapshot))
            return snapshot;

        return new OwnerCashStartingBalanceSnapshot();
    }

    private static decimal ResolveOwnerCashStartingBalance(
        IReadOnlyDictionary<string, OwnerCashStartingBalanceSnapshot> startingBalanceByKey,
        int officeId,
        Guid propertyId,
        Guid? ownerId) =>
        ResolveOwnerCashStartingBalanceSnapshot(startingBalanceByKey, officeId, propertyId, ownerId).LedgerBalance;

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

    private static OwnerCashStartingBalanceSnapshot CalculateOwnerApStartingBalanceSnapshot(
        IGrouping<string, JournalEntryLineSearchResult> group)
    {
        var orderedLines = group
            .OrderBy(line => line.TransactionDate)
            .ThenBy(line => line.JournalEntryCode)
            .ToList();
        var initialStartingBalanceLine = orderedLines
            .Where(line => IsOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo))
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .FirstOrDefault();
        if (initialStartingBalanceLine == null)
        {
            return new OwnerCashStartingBalanceSnapshot
            {
                LedgerBalance = orderedLines.Sum(line => line.Credit - line.Debit)
            };
        }

        var openingBalanceJeAmount = orderedLines
            .Where(line => line.JournalEntryId == initialStartingBalanceLine.JournalEntryId)
            .Sum(line => line.Credit - line.Debit);

        return new OwnerCashStartingBalanceSnapshot
        {
            LedgerBalance = orderedLines
                .Where(line => line.TransactionDate >= initialStartingBalanceLine.TransactionDate)
                .Sum(line => line.Credit - line.Debit),
            OpeningBalanceJeAmount = openingBalanceJeAmount,
            OpeningBalanceTransactionDate = initialStartingBalanceLine.TransactionDate
        };
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
