using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Owner AP Aging: load owner A/P lines via database proc (starting-balance cutoff), then API double-check.
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

        // Database filters pre-starting-balance lines; API double-checks before returning to UI.
        return await FilterOwnerApAgingJournalEntryLinesAsync(organizationId, lines.ToList());
    }

    /// <summary>
    /// Given owner A/P lines, resolve kind-110 starting balance transaction date per property and drop earlier lines.
    /// Used as an API safety net after <see cref="SearchOwnerApAgingJournalEntryLinesAsync"/> loads from the database proc.
    /// </summary>
    public async Task<IReadOnlyList<JournalEntryLineSearchResult>> FilterOwnerApAgingJournalEntryLinesAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines)
    {
        var lineList = (lines ?? Array.Empty<JournalEntryLineSearchResult>()).ToList();
        if (lineList.Count == 0)
            return lineList;

        var cutoffs = await ResolveOwnerApStartingBalanceCutoffByPropertyAsync(organizationId, lineList);

        return ApplyOwnerApOpeningBalanceCutoffFilter(lineList, cutoffs);
    }

    private async Task<Dictionary<string, DateOnly>> ResolveOwnerApStartingBalanceCutoffByPropertyAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines)
    {
        var cutoffs = BuildOwnerApOpeningBalanceCutoffByProperty(lines);
        await ApplyOwnerApOpeningBalanceCutoffsFromStartingBalancesAsync(organizationId, lines, cutoffs);
        return cutoffs;
    }

    private static Dictionary<string, DateOnly> BuildOwnerApOpeningBalanceCutoffByProperty(
        IEnumerable<JournalEntryLineSearchResult> lines)
    {
        var cutoffs = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Where(IsOwnerApStartingBalanceLine))
        {
            if (!TryGetOwnerApPropertyKey(line, out var propertyKey))
                continue;

            if (!cutoffs.TryGetValue(propertyKey, out var existing) || line.TransactionDate > existing)
                cutoffs[propertyKey] = line.TransactionDate;
        }

        return cutoffs;
    }

    private async Task ApplyOwnerApOpeningBalanceCutoffsFromStartingBalancesAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines,
        Dictionary<string, DateOnly> cutoffs)
    {
        var properties = lines
            .Where(line => line.PropertyId.HasValue && line.PropertyId.Value != Guid.Empty)
            .Select(line => (line.OfficeId, PropertyId: line.PropertyId!.Value))
            .Distinct()
            .ToList();

        foreach (var (officeId, propertyId) in properties)
        {
            var startingBalance = await GetOwnerStatementStartingBalanceAsync(organizationId, officeId, Guid.Empty, propertyId);
            if (startingBalance == null)
                continue;

            var propertyKey = GetOwnerApPropertyKey(officeId, propertyId);
            if (!cutoffs.TryGetValue(propertyKey, out var existing) || startingBalance.TransactionDate > existing)
                cutoffs[propertyKey] = startingBalance.TransactionDate;
        }
    }

    internal static IReadOnlyList<JournalEntryLineSearchResult> ApplyOwnerApOpeningBalanceCutoffFilter(
        IReadOnlyList<JournalEntryLineSearchResult> lines,
        IReadOnlyDictionary<string, DateOnly> cutoffs)
        => (lines ?? Array.Empty<JournalEntryLineSearchResult>())
            .Where(line => ShouldIncludeOwnerApAgingLine(line, cutoffs))
            .ToList();

    private static bool IsOwnerApStartingBalanceLine(JournalEntryLineSearchResult line)
        => line.JournalEntryKindId == (int)JournalEntryKind.OwnerStartingBalance;

    private static bool ShouldIncludeOwnerApAgingLine(
        JournalEntryLineSearchResult line,
        IReadOnlyDictionary<string, DateOnly> cutoffs)
    {
        if (!TryGetOwnerApPropertyKey(line, out var propertyKey))
            return true;

        if (!cutoffs.TryGetValue(propertyKey, out var cutoff))
            return true;

        return line.TransactionDate >= cutoff;
    }

    private static bool TryGetOwnerApPropertyKey(JournalEntryLineSearchResult line, out string propertyKey)
    {
        propertyKey = string.Empty;
        if (!line.PropertyId.HasValue || line.PropertyId.Value == Guid.Empty)
            return false;

        propertyKey = GetOwnerApPropertyKey(line.OfficeId, line.PropertyId.Value);
        return true;
    }

    private static string GetOwnerApPropertyKey(int officeId, Guid propertyId)
        => $"{officeId}|{propertyId}";

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
