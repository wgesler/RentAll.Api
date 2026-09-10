using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    private sealed class OwnerReportLoadedData
    {
        public RecapLineSet RecapLineSet { get; init; } = new();
        public List<PropertyReportData> Properties { get; init; } = [];
        public Dictionary<string, OwnerStartingBalance> StartingBalanceByKey { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, DateOnly> OpeningBalanceSheetCloseByOffice { get; init; } = new();
        public List<JournalEntryLineSearchResult> OwnerApLines { get; init; } = [];
        public List<int> OfficeIds { get; init; } = [];
        public List<OwnerInvoiceOutstanding> OutstandingInvoices { get; init; } = [];
    }

    public async Task<OwnerReportsBundle> GetOwnerReportsBundleAsync(JournalEntryRecapGetCriteria criteria)
    {
        var loaded = await LoadOwnerReportLoadedDataAsync(criteria);
        var cash = BuildOwnerCashReport(loaded, criteria);
        var accrual = BuildOwnerAccrualReport(loaded, criteria);
        var recap = new RecapReport
        {
            Rows = BuildRecapReportRows(loaded.RecapLineSet.AllLines),
            RentalIncomeParentAccountNo = ResolveRentalIncomeParentAccountNoLabel(criteria)
        };

        return new OwnerReportsBundle
        {
            Cash = cash,
            Accrual = accrual,
            Recap = recap,
            OutstandingInvoices = loaded.OutstandingInvoices
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
        await EnsureRentalIncomeParentAccountIdsAsync(criteria);

        var bundle = await _journalEntryRepository.GetOwnerReportBundleDataAsync(criteria, priorMonthClose, periodStart);
        var openingBalanceSheetCloseByOffice = ResolveOpeningBalanceSheetCloseDateByOffice(bundle.OwnerApLines);
        var recapLines = bundle.RecapLines;
        var forwardRecapLoadStart = ResolveOwnerCashForwardRecapLoadStartDate(criteria, openingBalanceSheetCloseByOffice);
        if (forwardRecapLoadStart.HasValue)
        {
            var reportEndDate = GetReportPeriodEndDate(criteria.StartDate, criteria.EndDate)
                ?? criteria.EndDate
                ?? criteria.StartDate
                ?? forwardRecapLoadStart.Value;
            var forwardCriteria = CloneJournalEntryRecapCriteriaWithDates(criteria, forwardRecapLoadStart.Value, reportEndDate);
            var forwardBundle = await _journalEntryRepository.GetOwnerReportBundleDataAsync(
                forwardCriteria,
                GetPriorMonthCloseDate(forwardCriteria.StartDate, forwardCriteria.EndDate),
                GetReportPeriodStartDate(forwardCriteria.StartDate, forwardCriteria.EndDate));
            recapLines = MergeRecapLinesByJournalEntryLineId(forwardBundle.RecapLines, recapLines);
        }

        var recapLineSet = new RecapLineSet
        {
            AllLines = recapLines,
            ActivityLines = FilterRecapLinesToReportMonthActivity(recapLines, criteria)
        };

        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        if (officeIds.Count == 0)
        {
            return new OwnerReportLoadedData
            {
                RecapLineSet = recapLineSet,
                OfficeIds = officeIds
            };
        }

        var properties = await LoadOwnerPropertyReportDataAsync(criteria);
        var startingBalanceByKey = BuildOwnerStartingBalanceByProperty(criteria, officeIds, bundle.OwnerApLines);
        var outstandingInvoices = (await _accountingRepository.GetOwnerInvoiceOutstandingByCriteriaAsync(criteria.OrganizationId, criteria.PropertyId, string.IsNullOrWhiteSpace(criteria.OfficeIds) ? null : criteria.OfficeIds, criteria.EndDate)).ToList();
        return new OwnerReportLoadedData
        {
            RecapLineSet = recapLineSet,
            Properties = properties,
            StartingBalanceByKey = startingBalanceByKey,
            OpeningBalanceSheetCloseByOffice = openingBalanceSheetCloseByOffice,
            OwnerApLines = bundle.OwnerApLines,
            OfficeIds = officeIds,
            OutstandingInvoices = outstandingInvoices
        };
    }
}
