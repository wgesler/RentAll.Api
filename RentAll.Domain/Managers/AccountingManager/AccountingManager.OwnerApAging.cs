using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    /// <summary>
    /// Owner AP Aging: include opening balance and later activity per property; exclude earlier JEs.
    /// </summary>
    public async Task<IReadOnlyList<JournalEntryLineSearchResult>> FilterOwnerApAgingJournalEntryLinesAsync(
        Guid organizationId,
        IReadOnlyList<JournalEntryLineSearchResult> lines)
    {
        var lineList = (lines ?? Array.Empty<JournalEntryLineSearchResult>()).ToList();
        if (lineList.Count == 0)
            return lineList;

        var cutoffs = BuildOwnerApOpeningBalanceCutoffByProperty(lineList);
        await ApplyOwnerApOpeningBalanceCutoffsFromStartingBalancesAsync(organizationId, lineList, cutoffs);

        return lineList
            .Where(line => ShouldIncludeOwnerApAgingLine(line, cutoffs))
            .ToList();
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
        var missingProperties = lines
            .Where(line => TryGetOwnerApPropertyKey(line, out var key) && !cutoffs.ContainsKey(key))
            .Select(line => (line.OfficeId, PropertyId: line.PropertyId!.Value))
            .Distinct()
            .ToList();

        foreach (var (officeId, propertyId) in missingProperties)
        {
            var startingBalance = await GetOwnerStatementStartingBalanceAsync(organizationId, officeId, Guid.Empty, propertyId);
            if (startingBalance == null)
                continue;

            var propertyKey = GetOwnerApPropertyKey(officeId, propertyId);
            if (!cutoffs.TryGetValue(propertyKey, out var existing) || startingBalance.TransactionDate > existing)
                cutoffs[propertyKey] = startingBalance.TransactionDate;
        }
    }

    private static bool IsOwnerApStartingBalanceLine(JournalEntryLineSearchResult line)
    {
        if (line.JournalEntryKindId == (int)JournalEntryKind.OwnerStartingBalance)
            return true;

        return MatchOwnerStartingBalanceMemo(line.JournalEntryMemo, line.Memo).IsMatch;
    }

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
}
