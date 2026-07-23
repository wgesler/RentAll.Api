using RentAll.Domain.Enums;
using RentAll.Domain.Models;
using System.Globalization;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class EscrowReportLoadedData
    {
        public RecapLineSet RecapLineSet { get; init; } = new();
        public List<PropertyReportData> Properties { get; init; } = [];
        public List<int> OfficeIds { get; init; } = [];
        public List<EscrowOfficeBalance> EscrowOfficeBalances { get; init; } = [];
        public Dictionary<string, decimal> EscrowPrepaidByPropertyKey { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<EscrowReport> GetEscrowReportAsync(JournalEntryRecapGetCriteria criteria, decimal cushion)
    {
        var normalizedCriteria = NormalizeEscrowReportCriteria(criteria);
        var loaded = await LoadEscrowReportLoadedDataAsync(normalizedCriteria);
        var accrualReport = BuildEscrowAccrualReport(loaded);
        return BuildEscrowReport(loaded, normalizedCriteria, accrualReport, cushion);
    }

    private async Task<EscrowReportLoadedData> LoadEscrowReportLoadedDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        criteria.IncludePaymentInvoiceContext = true;
        var bundle = await _journalEntryRepository.GetEscrowReportDataAsync(criteria);
        var recapLineSet = new RecapLineSet
        {
            AllLines = bundle.RecapLines,
            ActivityLines = bundle.RecapLines.Where(line => line.IsInDateRange).ToList()
        };

        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
        {
            return new EscrowReportLoadedData
            {
                RecapLineSet = recapLineSet,
                OfficeIds = officeIds,
                EscrowOfficeBalances = bundle.EscrowOfficeBalances,
                EscrowPrepaidByPropertyKey = BuildEscrowPrepaidByPropertyKey(bundle.EscrowPrepaidPropertyBalances)
            };
        }

        var properties = await LoadOwnerPropertyReportDataAsync(criteria);
        return new EscrowReportLoadedData
        {
            RecapLineSet = recapLineSet,
            Properties = properties,
            OfficeIds = officeIds,
            EscrowOfficeBalances = bundle.EscrowOfficeBalances,
            EscrowPrepaidByPropertyKey = BuildEscrowPrepaidByPropertyKey(bundle.EscrowPrepaidPropertyBalances)
        };
    }

    private OwnerAccrualReport BuildEscrowAccrualReport(EscrowReportLoadedData loaded)
    {
        if (loaded.OfficeIds.Count == 0)
            return new OwnerAccrualReport();

        var propertyActivityLines = BuildOwnerActivityLines(
            loaded.RecapLineSet.ActivityLines,
            loaded.RecapLineSet.AllLines,
            OwnerReportActivityMode.Accrual);
        var activityLinesByProperty = BuildOwnerActivityLinesByProperty(propertyActivityLines);

        var rows = loaded.Properties
            .Select(property =>
            {
                var propertyKey = GetPropertyReportKey(property.OfficeId, property.PropertyId);
                activityLinesByProperty.TryGetValue(propertyKey, out var activityLines);
                activityLines ??= [];

                var invoicedIncome = activityLines.Sum(line => line.ExpectedIncome);
                var paidIncome = activityLines.Sum(line => line.ReceivedIncome);
                var unpaidIncome = CalculateUnpaidIncome(invoicedIncome, paidIncome);

                return new OwnerAccrualReportRow
                {
                    PropertyId = property.PropertyId,
                    OfficeId = property.OfficeId,
                    OfficeName = property.OfficeName,
                    OwnerId = property.PrimaryOwnerId,
                    PropertyCode = property.PropertyCode,
                    CompanyName = property.CompanyName,
                    OwnerNames = property.OwnerNames,
                    OwnerNameLine = property.OwnerNameLine,
                    InvoicedIncome = invoicedIncome,
                    UnpaidIncome = unpaidIncome
                };
            })
            .OrderBy(row => row.OfficeName)
            .ThenBy(row => row.PropertyCode)
            .ToList();

        return new OwnerAccrualReport
        {
            Rows = rows
        };
    }

    private EscrowReport BuildEscrowReport(
        EscrowReportLoadedData loaded,
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

    private static Dictionary<string, decimal> BuildEscrowPrepaidByPropertyKey(IEnumerable<EscrowPrepaidPropertyBalance> balances)
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
