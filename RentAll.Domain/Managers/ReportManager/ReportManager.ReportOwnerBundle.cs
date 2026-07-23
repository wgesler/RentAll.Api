using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class OwnerReportLoadedData
    {
        public RecapLineSet RecapLineSet { get; init; } = new();
        public List<PropertyReportData> Properties { get; init; } = [];
        public Dictionary<string, OwnerStartingBalance> StartingBalanceByKey { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<int> OfficeIds { get; init; } = [];
        public List<EscrowOfficeBalance> EscrowOfficeBalances { get; init; } = [];
    }

    public async Task<OwnerReportsBundle> GetOwnerReportsBundleAsync(JournalEntryRecapGetCriteria criteria)
    {
        var loaded = await LoadOwnerReportLoadedDataAsync(criteria);
        var cash = BuildOwnerCashReport(loaded, criteria);
        var accrual = BuildOwnerAccrualReport(loaded, criteria);
        var recapRows = BuildRecapReportRows(loaded.RecapLineSet.AllLines);
        var escrow = BuildEscrowReport(loaded, criteria, accrual, recapRows, cushion: 0m);

        return new OwnerReportsBundle
        {
            Cash = cash,
            Accrual = accrual,
            Recap = new RecapReport
            {
                Rows = recapRows
            },
            Escrow = escrow
        };
    }

    public async Task<OwnerCashReport> GetOwnerCashReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var loaded = await LoadOwnerReportLoadedDataAsync(criteria);
        return BuildOwnerCashReport(loaded, criteria);
    }

    public async Task<OwnerAccrualReport> GetOwnerAccrualReportAsync(JournalEntryRecapGetCriteria criteria)
    {
        var loaded = await LoadOwnerReportLoadedDataAsync(criteria);
        return BuildOwnerAccrualReport(loaded, criteria);
    }

    private async Task<OwnerReportLoadedData> LoadOwnerReportLoadedDataAsync(JournalEntryRecapGetCriteria criteria)
    {
        var priorMonthClose = GetPriorMonthCloseDate(criteria.StartDate, criteria.EndDate);
        var periodStart = GetReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        criteria.IncludePaymentInvoiceContext = true;

        var bundle = await _journalEntryRepository.GetOwnerReportBundleDataAsync(criteria, priorMonthClose, periodStart);
        var recapLineSet = new RecapLineSet
        {
            AllLines = bundle.RecapLines,
            ActivityLines = bundle.RecapLines.Where(line => line.IsInDateRange).ToList()
        };

        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
        {
            return new OwnerReportLoadedData
            {
                RecapLineSet = recapLineSet,
                OfficeIds = officeIds,
                EscrowOfficeBalances = bundle.EscrowOfficeBalances
            };
        }

        var properties = await LoadOwnerPropertyReportDataAsync(criteria);
        var startingBalanceByKey = BuildOwnerStartingBalanceByProperty(criteria, officeIds, bundle.OwnerApLines);
        return new OwnerReportLoadedData
        {
            RecapLineSet = recapLineSet,
            Properties = properties,
            StartingBalanceByKey = startingBalanceByKey,
            OfficeIds = officeIds,
            EscrowOfficeBalances = bundle.EscrowOfficeBalances
        };
    }
}
