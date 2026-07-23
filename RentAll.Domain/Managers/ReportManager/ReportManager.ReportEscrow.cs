using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<EscrowReport> GetEscrowReportAsync(JournalEntryRecapGetCriteria criteria, decimal cushion)
    {
        var normalizedCriteria = NormalizeEscrowReportCriteria(criteria);
        var loaded = await LoadOwnerReportLoadedDataAsync(normalizedCriteria);
        var accrualReport = BuildOwnerAccrualReport(loaded, normalizedCriteria);
        return BuildEscrowReport(loaded, normalizedCriteria, accrualReport, cushion);
    }

    private EscrowReport BuildEscrowReport(
        OwnerReportLoadedData loaded,
        JournalEntryRecapGetCriteria criteria,
        OwnerAccrualReport accrualReport,
        decimal cushion)
    {
        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        var (escrowOwnersBalance, escrowOwnersAccountLabel) = ResolveEscrowOwnersAccountBalance(
            loaded.EscrowOfficeBalances,
            officeIds);

        return BuildEscrowReport(
            accrualReport.Rows,
            loaded.EscrowPrepaidByPropertyKey,
            criteria.PropertyId,
            criteria.EndDate,
            ResolveEscrowEntityLineLabel(accrualReport.Rows, officeIds.Count),
            cushion,
            escrowOwnersBalance,
            escrowOwnersAccountLabel);
    }

    private static JournalEntryRecapGetCriteria NormalizeEscrowReportCriteria(JournalEntryRecapGetCriteria criteria)
    {
        var endDate = criteria.EndDate
            ?? throw new ArgumentException("EndDate is required for the escrow report.");

        var startDate = criteria.StartDate ?? new DateOnly(endDate.Year, 1, 1);
        if (endDate < startDate)
            throw new ArgumentException("EndDate must be on or after StartDate.");

        return new JournalEntryRecapGetCriteria
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            ReservationId = criteria.ReservationId,
            StartDate = startDate,
            EndDate = endDate,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted
        };
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

    private static string? ResolveEscrowEntityLineLabel(IReadOnlyList<OwnerAccrualReportRow> rows, int officeCount)
    {
        if (officeCount != 1)
            return "All Offices";

        return rows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.OfficeName))?.OfficeName;
    }

    private static EscrowReport BuildEscrowReport(
        IReadOnlyList<OwnerAccrualReportRow> accrualRows,
        IReadOnlyDictionary<string, decimal> prepaidByPropertyKey,
        Guid? propertyId,
        DateOnly? asOfDate,
        string? entityLineLabel,
        decimal cushion,
        decimal escrowBankBalance,
        string escrowBankAccountLabel)
    {
        var rows = (accrualRows ?? [])
            .Where(row => !propertyId.HasValue || propertyId.Value == Guid.Empty || row.PropertyId == propertyId.Value)
            .Select(row =>
            {
                var propertyKey = GetPropertyReportKey(row.OfficeId, row.PropertyId);
                var arBalance = RoundFinancialReportAmount(row.InvoicedIncome);
                var prepaidRaw = prepaidByPropertyKey.TryGetValue(propertyKey, out var prepaidBalance)
                    ? prepaidBalance
                    : 0m;
                var prepaids = RoundFinancialReportAmount(prepaidRaw);
                var notCollected = RoundFinancialReportAmount(row.UnpaidIncome);
                var total = RoundFinancialReportAmount(arBalance - prepaids - notCollected);
                var e2 = total < 0m ? 0m : total;

                return new EscrowReportRow
                {
                    RowId = $"{row.OfficeId}-{propertyKey}",
                    OwnerName = ResolveEscrowOwnerName(row),
                    PropertyId = row.PropertyId,
                    PropertyCode = string.IsNullOrWhiteSpace(row.PropertyCode) ? "—" : row.PropertyCode.Trim(),
                    OfficeId = row.OfficeId,
                    ArBalance = arBalance,
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
        var roundedBankBalance = RoundFinancialReportAmount(escrowBankBalance);

        return new EscrowReport
        {
            ReportTitle = "Escrow Report",
            PeriodLabel = asOfDate.HasValue
                ? $"As of {asOfDate.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)}"
                : string.Empty,
            EntityLineLabel = string.IsNullOrWhiteSpace(entityLineLabel) ? null : entityLineLabel.Trim(),
            Rows = rows,
            Totals = totals,
            Cushion = roundedCushion,
            EscrowBankBalance = roundedBankBalance,
            EscrowBankAccountLabel = string.IsNullOrWhiteSpace(escrowBankAccountLabel)
                ? "Escrow Owners"
                : escrowBankAccountLabel.Trim(),
            Transfer = RoundFinancialReportAmount(roundedBankBalance + totals.Total - roundedCushion)
        };
    }

    private static string ResolveEscrowOwnerName(OwnerAccrualReportRow row)
    {
        var ownerName = (row.OwnerNames ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerName))
            return ownerName;

        ownerName = (row.OwnerNameLine ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(ownerName))
            return ownerName;

        ownerName = (row.CompanyName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(ownerName) ? "—" : ownerName;
    }

    private static decimal RoundFinancialReportAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
