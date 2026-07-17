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
        var recapRows = BuildRecapReportRows(loaded.RecapLineSet.AllLines);
        var officeIds = GetReportOfficeIds(normalizedCriteria.OfficeIds);
        var (escrowOwnersBalance, escrowOwnersAccountLabel) = await LoadEscrowOwnersAccountBalanceAsync(
            normalizedCriteria.OrganizationId,
            officeIds,
            normalizedCriteria.EndDate);

        return BuildEscrowReport(
            accrualReport.Rows,
            recapRows,
            normalizedCriteria.PropertyId,
            normalizedCriteria.EndDate,
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

    private async Task<(decimal Balance, string AccountLabel)> LoadEscrowOwnersAccountBalanceAsync(
        Guid organizationId,
        IReadOnlyList<int> officeIds,
        DateOnly? asOfDate)
    {
        decimal totalBalance = 0m;
        string? accountLabel = null;

        foreach (var officeId in officeIds)
        {
            var chartOfAccounts = (await _accountingRepository.GetChartOfAccountsByOfficeIdAsync(organizationId, officeId)).ToList();
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
            var accountId = _accountingManager.GetDefaultEscrowOwnersAccount(chartOfAccounts, officeId, accountingOffice);

            var lines = await _journalEntryRepository.GetJournalEntryLinesAsync(new JournalEntryLineGetCriteria
            {
                OrganizationId = organizationId,
                OfficeIds = officeId.ToString(CultureInfo.InvariantCulture),
                ChartOfAccountId = accountId,
                IncludeVoided = false,
                IncludeUnposted = true,
                StartDate = null,
                EndDate = asOfDate
            });

            totalBalance += SumEscrowLiabilityAccountBalance(lines);
            accountLabel ??= FormatChartOfAccountLabel(chartOfAccounts.FirstOrDefault(account => account.AccountId == accountId));
        }

        return (RoundFinancialReportAmount(totalBalance), accountLabel ?? "Escrow Owners");
    }

    private static decimal SumEscrowLiabilityAccountBalance(IEnumerable<JournalEntryLineSearchResult> lines)
        => RoundFinancialReportAmount((lines ?? []).Sum(line => line.Credit - line.Debit));

    private static string FormatChartOfAccountLabel(ChartOfAccount? account)
    {
        if (account == null)
            return string.Empty;

        return $"{account.AccountNo} {account.Name}".Trim();
    }

    private static string? ResolveEscrowEntityLineLabel(IReadOnlyList<OwnerAccrualReportRow> rows, int officeCount)
    {
        if (officeCount != 1)
            return "All Offices";

        return rows.FirstOrDefault(row => !string.IsNullOrWhiteSpace(row.OfficeName))?.OfficeName;
    }

    private static EscrowReport BuildEscrowReport(
        IReadOnlyList<OwnerAccrualReportRow> accrualRows,
        IReadOnlyList<RecapReportRow> recapRows,
        Guid? propertyId,
        DateOnly? asOfDate,
        string? entityLineLabel,
        decimal cushion,
        decimal escrowBankBalance,
        string escrowBankAccountLabel)
    {
        var lastRecapByProperty = BuildEscrowLastRecapAmountsByProperty(recapRows);
        var rows = (accrualRows ?? [])
            .Where(row => !propertyId.HasValue || propertyId.Value == Guid.Empty || row.PropertyId == propertyId.Value)
            .Select(row =>
            {
                var propertyKey = row.PropertyId.ToString("D");
                lastRecapByProperty.TryGetValue(propertyKey, out var recapAmounts);
                var arBalance = RoundFinancialReportAmount(row.UnpaidIncome);
                var prepaids = RoundFinancialReportAmount(recapAmounts?.Prepaids ?? row.PrepaidIncome);
                var notCollected = RoundFinancialReportAmount(recapAmounts?.NotCollected ?? 0m);
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

    private sealed class EscrowRecapPropertyAmounts
    {
        public decimal Prepaids { get; set; }
        public decimal NotCollected { get; set; }
    }

    private static Dictionary<string, EscrowRecapPropertyAmounts> BuildEscrowLastRecapAmountsByProperty(
        IReadOnlyList<RecapReportRow> recapRows)
    {
        var byPropertyReservation = new Dictionary<string, RecapReportRow>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in recapRows ?? [])
        {
            if (!row.PropertyId.HasValue || row.PropertyId.Value == Guid.Empty)
                continue;

            var propertyId = row.PropertyId.Value.ToString("D");
            var reservationKey = (row.ReservationCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reservationKey))
                reservationKey = "no-reservation";

            var key = $"{propertyId}|{reservationKey}";
            if (!byPropertyReservation.TryGetValue(key, out var existing))
            {
                byPropertyReservation[key] = row;
                continue;
            }

            var periodCompare = string.Compare(row.AccountingPeriod, existing.AccountingPeriod, StringComparison.OrdinalIgnoreCase);
            if (periodCompare > 0 || (periodCompare == 0 && row.SortDateValue >= existing.SortDateValue))
                byPropertyReservation[key] = row;
        }

        var totals = new Dictionary<string, EscrowRecapPropertyAmounts>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in byPropertyReservation.Values)
        {
            if (!row.PropertyId.HasValue || row.PropertyId.Value == Guid.Empty)
                continue;

            var propertyId = row.PropertyId.Value.ToString("D");
            if (!totals.TryGetValue(propertyId, out var existing))
            {
                existing = new EscrowRecapPropertyAmounts();
                totals[propertyId] = existing;
            }

            existing.Prepaids = RoundFinancialReportAmount(existing.Prepaids + row.PrePaymentValue);
            existing.NotCollected = RoundFinancialReportAmount(existing.NotCollected + row.UnPaidValue);
        }

        return totals;
    }

    private static decimal RoundFinancialReportAmount(decimal amount)
        => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
