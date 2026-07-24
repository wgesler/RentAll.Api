using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Owner AP Aging: load owner A/P lines via database proc (opening balance sheet cutoff), then API double-check.
    /// </summary>
    public async Task<IReadOnlyList<JournalEntryLineSearchResult>> SearchOwnerApAgingJournalEntryLinesAsync(
        Guid organizationId,
        IReadOnlyList<int> officeIds,
        DateOnly? endDate,
        bool includeVoided = false,
        bool includeUnposted = true)
    {
        if (!await IsAccountingFeatureEnabledAsync(organizationId))
            return [];

        var distinctOfficeIds = (officeIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
        if (distinctOfficeIds.Count == 0)
            return [];

        var accountIds = new HashSet<int>();
        foreach (var officeId in distinctOfficeIds)
        {
            var (chartOfAccounts, accountingOffice) = await LoadAccountContextAsync(organizationId, officeId);
            foreach (var accountId in ResolveOwnerApAccountIds(chartOfAccounts, officeId, accountingOffice))
                accountIds.Add(accountId);
        }

        if (accountIds.Count == 0)
            return [];

        var lines = await _journalEntryRepository.GetOwnerApAgingJournalEntryLinesAsync(new JournalEntryLineOwnerApAgingGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = string.Join(',', distinctOfficeIds),
            ChartOfAccountIds = string.Join(',', accountIds),
            IncludeVoided = includeVoided,
            IncludeUnposted = includeUnposted,
            EndDate = endDate
        });

        // Database filters pre-opening-balance-sheet lines; API double-checks before returning to UI.
        return await FilterOwnerApAgingJournalEntryLinesAsync(organizationId, lines.ToList());
    }

    /// <summary>
    /// Given owner A/P lines, resolve the office Opening Balance Sheet transaction date and drop earlier lines.
    /// Used as an API safety net after <see cref="SearchOwnerApAgingJournalEntryLinesAsync"/> loads from the database proc.
    /// </summary>
    public async Task<IReadOnlyList<JournalEntryLineSearchResult>> FilterOwnerApAgingJournalEntryLinesAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines)
    {
        var lineList = (lines ?? Array.Empty<JournalEntryLineSearchResult>()).ToList();
        if (lineList.Count == 0)
            return lineList;

        var cutoffs = await ResolveOwnerApOpeningBalanceCutoffByOfficeAsync(organizationId, lineList);
        return ApplyOwnerApOpeningBalanceCutoffFilter(lineList, cutoffs);
    }

    private async Task<Dictionary<int, DateOnly>> ResolveOwnerApOpeningBalanceCutoffByOfficeAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines)
    {
        var officeIds = lines
            .Select(line => line.OfficeId)
            .Where(officeId => officeId > 0)
            .Distinct()
            .ToList();

        if (officeIds.Count == 0)
            return new Dictionary<int, DateOnly>();

        var cutoffs = new Dictionary<int, DateOnly>();
        foreach (var officeId in officeIds)
        {
            var openingBalanceSheetDate = await GetLatestOpeningBalanceSheetTransactionDateAsync(organizationId, officeId);
            if (openingBalanceSheetDate != default)
                cutoffs[officeId] = openingBalanceSheetDate;
        }

        return cutoffs;
    }

    private async Task<DateOnly> GetLatestOpeningBalanceSheetTransactionDateAsync(Guid organizationId, int officeId)
    {
        var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeId.ToString(),
            IncludeVoided = false,
            IncludeUnposted = true,
            IncludeCashOnly = true
        });

        return lines
            .Where(line => line.JournalEntryKindId == (int)JournalEntryKind.OpeningBalanceSheet)
            .OrderByDescending(line => line.TransactionDate)
            .ThenByDescending(line => line.JournalEntryCode)
            .Select(line => line.TransactionDate)
            .FirstOrDefault();
    }

    internal static IReadOnlyList<JournalEntryLineSearchResult> ApplyOwnerApOpeningBalanceCutoffFilter(
        IReadOnlyList<JournalEntryLineSearchResult> lines,
        IReadOnlyDictionary<int, DateOnly> cutoffs)
        => (lines ?? Array.Empty<JournalEntryLineSearchResult>())
            .Where(line => ShouldIncludeOwnerApAgingLine(line, cutoffs))
            .ToList();

    private static bool ShouldIncludeOwnerApAgingLine(
        JournalEntryLineSearchResult line,
        IReadOnlyDictionary<int, DateOnly> cutoffs)
    {
        if (!cutoffs.TryGetValue(line.OfficeId, out var cutoff))
            return true;

        return line.TransactionDate >= cutoff;
    }

    /// <summary>
    /// Owner AP Aging account scope: configured owner A/P plus chart account No 2001 when present.
    /// </summary>
    internal IReadOnlyList<int> ResolveOwnerApAccountIds(List<ChartOfAccount> chartOfAccounts, int officeId, AccountingOffice? accountingOffice)
    {
        var accountIds = new HashSet<int>();
        if (accountingOffice?.DefaultOwnActPayableAccountId is > 0)
            accountIds.Add(accountingOffice.DefaultOwnActPayableAccountId.Value);

        foreach (var account in chartOfAccounts.Where(account => account.OfficeId == officeId))
        {
            var normalizedAccountNo = (account.AccountNo ?? string.Empty).Trim().TrimStart('0');
            if (normalizedAccountNo == "2001")
                accountIds.Add(account.AccountId);
        }

        if (accountIds.Count == 0)
            accountIds.Add(GetDefaultOwnerAccountsPayable(chartOfAccounts, officeId, accountingOffice));

        return accountIds.ToList();
    }
}
