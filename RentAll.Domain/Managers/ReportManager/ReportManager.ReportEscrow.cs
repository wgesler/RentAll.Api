using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<EscrowReport> GetEscrowReportAsync(JournalEntryRecapGetCriteria criteria, decimal cushion)
    {
        var normalizedCriteria = NormalizeEscrowReportCriteria(criteria);
        var bundle = await _journalEntryRepository.GetEscrowReportDataAsync(normalizedCriteria);
        return BuildEscrowReport(bundle, normalizedCriteria, cushion);
    }

    private static EscrowReport BuildEscrowReport(
        EscrowReportBundleData bundle,
        JournalEntryRecapGetCriteria criteria,
        decimal cushion)
    {
        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        var prepaidByPropertyKey = BuildEscrowPrepaidByPropertyKey(bundle.PrepaidPropertyBalances);
        var notCollectedByPropertyKey = BuildEscrowNotCollectedByPropertyKey(bundle.NotCollectedPropertyBalances);
        var escrowOfficeBalances = FilterEscrowOfficeBalances(bundle.EscrowOfficeBalances, officeIds);
        var (escrowOwnersBalance, escrowOwnersAccountLabel) = ResolveEscrowOwnersAccountBalance(
            escrowOfficeBalances,
            officeIds);

        var rows = (bundle.Properties ?? [])
            .Where(property => !criteria.PropertyId.HasValue
                || criteria.PropertyId.Value == Guid.Empty
                || property.PropertyId == criteria.PropertyId.Value)
            .Select(property =>
            {
                var propertyKey = GetPropertyReportKey(property.OfficeId, property.PropertyId);
                var apBalance = RoundFinancialReportAmount(property.ApBalance);
                var prepaids = prepaidByPropertyKey.TryGetValue(propertyKey, out var prepaidBalance)
                    ? RoundFinancialReportAmount(prepaidBalance)
                    : 0m;
                var notCollected = notCollectedByPropertyKey.TryGetValue(propertyKey, out var notCollectedBalance)
                    ? RoundFinancialReportAmount(notCollectedBalance)
                    : 0m;
                var total = RoundFinancialReportAmount(apBalance + prepaids - notCollected);
                var e2 = total > 0m ? total : 0m;

                return new EscrowReportRow
                {
                    RowId = $"{property.OfficeId}-{propertyKey}",
                    OwnerName = ResolveEscrowOwnerName(property),
                    PropertyId = property.PropertyId,
                    PropertyCode = string.IsNullOrWhiteSpace(property.PropertyCode) ? "—" : property.PropertyCode.Trim(),
                    OfficeId = property.OfficeId,
                    ArBalance = apBalance,
                    Prepaids = prepaids,
                    NotCollected = notCollected,
                    Total = total,
                    E2 = e2
                };
            })
            .Where(row =>
                Math.Abs(row.ArBalance) > 0.005m
                || Math.Abs(row.Prepaids) > 0.005m
                || Math.Abs(row.NotCollected) > 0.005m
                || Math.Abs(row.Total) > 0.005m)
            .OrderBy(row => row.OwnerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.PropertyCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totals = rows.Aggregate(
            new EscrowReportTotals(),
            (acc, row) => new EscrowReportTotals
            {
                ArBalance = RoundFinancialReportAmount(acc.ArBalance + row.ArBalance),
                Prepaids = RoundFinancialReportAmount(acc.Prepaids + row.Prepaids),
                NotCollected = RoundFinancialReportAmount(acc.NotCollected + row.NotCollected),
                Total = RoundFinancialReportAmount(acc.Total + row.Total),
                E2 = RoundFinancialReportAmount(acc.E2 + row.E2)
            });

        var roundedCushion = RoundFinancialReportAmount(cushion);
        var roundedBankBalance = RoundFinancialReportAmount(escrowOwnersBalance);

        return new EscrowReport
        {
            ReportTitle = "Escrow Report",
            PeriodLabel = criteria.EndDate.HasValue
                ? $"As of {criteria.EndDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}"
                : string.Empty,
            EntityLineLabel = ResolveEscrowEntityLineLabel(bundle.Properties ?? [], officeIds.Count),
            Rows = rows,
            Totals = totals,
            Cushion = roundedCushion,
            EscrowBankBalance = roundedBankBalance,
            EscrowBankAccountLabel = string.IsNullOrWhiteSpace(escrowOwnersAccountLabel)
                ? "Escrow Owners"
                : escrowOwnersAccountLabel.Trim(),
            EscrowOfficeBalances = escrowOfficeBalances,
            Transfer = RoundFinancialReportAmount(roundedBankBalance + totals.Total - roundedCushion)
        };
    }

    private static JournalEntryRecapGetCriteria NormalizeEscrowReportCriteria(JournalEntryRecapGetCriteria criteria)
    {
        var endDate = criteria.EndDate
            ?? throw new ArgumentException("EndDate is required for the escrow report.");

        return new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = null,
            EndDate = endDate,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted
        };
    }

    private static Dictionary<string, decimal> BuildEscrowPrepaidByPropertyKey(
        IEnumerable<EscrowPrepaidPropertyBalance> balances)
    {
        var byKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var balance in balances ?? [])
        {
            if (balance.PropertyId == Guid.Empty)
                continue;

            var key = GetPropertyReportKey(balance.OfficeId, balance.PropertyId);
            byKey[key] = Math.Round(balance.Balance, 2, MidpointRounding.AwayFromZero);
        }

        return byKey;
    }

    private static Dictionary<string, decimal> BuildEscrowNotCollectedByPropertyKey(
        IEnumerable<EscrowNotCollectedPropertyBalance> balances)
    {
        var byKey = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var balance in balances ?? [])
        {
            if (balance.PropertyId == Guid.Empty)
                continue;

            var key = GetPropertyReportKey(balance.OfficeId, balance.PropertyId);
            byKey[key] = Math.Round(balance.NotCollectedAmount, 2, MidpointRounding.AwayFromZero);
        }

        return byKey;
    }

    private static List<EscrowOfficeBalance> FilterEscrowOfficeBalances(
        IEnumerable<EscrowOfficeBalance> escrowOfficeBalances,
        IReadOnlyList<int> officeIds)
    {
        if (officeIds.Count == 0)
            return [];

        var officeIdSet = officeIds.ToHashSet();
        return (escrowOfficeBalances ?? [])
            .Where(balance => officeIdSet.Contains(balance.OfficeId))
            .Select(balance => new EscrowOfficeBalance
            {
                OfficeId = balance.OfficeId,
                AccountId = balance.AccountId,
                AccountNo = balance.AccountNo,
                AccountName = balance.AccountName,
                Balance = RoundFinancialReportAmount(balance.Balance)
            })
            .OrderBy(balance => balance.OfficeId)
            .ToList();
    }

    private static (decimal Balance, string AccountLabel) ResolveEscrowOwnersAccountBalance(
        IReadOnlyList<EscrowOfficeBalance> escrowOfficeBalances,
        IReadOnlyList<int> officeIds)
    {
        if (officeIds.Count == 0 || escrowOfficeBalances.Count == 0)
            return (0m, "Escrow Owners");

        var officeIdSet = officeIds.ToHashSet();
        var balances = escrowOfficeBalances
            .Where(balance => officeIdSet.Contains(balance.OfficeId))
            .ToList();

        var totalBalance = RoundFinancialReportAmount(balances.Sum(balance => balance.Balance));
        var firstBalance = balances.FirstOrDefault();
        var accountLabel = firstBalance == null
            ? "Escrow Owners"
            : FormatEscrowAccountLabel(firstBalance.AccountNo, firstBalance.AccountName);

        return (totalBalance, accountLabel);
    }

    private static string FormatEscrowAccountLabel(string accountNo, string accountName)
    {
        var label = $"{accountNo} {accountName}".Trim();
        return string.IsNullOrWhiteSpace(label) ? "Escrow Owners" : label;
    }

    private static string? ResolveEscrowEntityLineLabel(IReadOnlyList<EscrowPropertyReportData> properties, int officeCount)
    {
        if (officeCount != 1)
            return "All Offices";

        return properties.FirstOrDefault(property => !string.IsNullOrWhiteSpace(property.OfficeName))?.OfficeName;
    }

    private static string ResolveEscrowOwnerName(EscrowPropertyReportData property)
    {
        var ownerName = (property.OwnerNames ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerName))
            return ownerName;

        ownerName = (property.OwnerNameLine ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerName))
            return ownerName;

        ownerName = (property.CompanyName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(ownerName) ? "—" : ownerName;
    }

    private static decimal RoundFinancialReportAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
