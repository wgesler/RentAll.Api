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
        public Dictionary<string, OwnerReportMonthlyAnchor> PriorMonthAnchorByKey { get; init; } = new(StringComparer.OrdinalIgnoreCase);
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
        criteria.IncludePaymentInvoiceContext = true;
        await EnsureRentalIncomeParentAccountIdsAsync(criteria);

        var officeIds = GetReportOfficeIds(criteria.OfficeIds);
        var officeIdsCsv = string.IsNullOrWhiteSpace(criteria.OfficeIds)
            ? string.Join(",", officeIds)
            : criteria.OfficeIds;
        var periodStart = GetReportPeriodStartDate(criteria.StartDate, criteria.EndDate);
        var reportEndDate = GetReportPeriodEndDate(criteria.StartDate, criteria.EndDate)
            ?? criteria.EndDate
            ?? criteria.StartDate;

        var loadCriteria = CloneOwnerReportBundleLoadCriteria(criteria);
        if (periodStart.HasValue && officeIds.Count > 0)
        {
            var loadPlan = await _accountingRepository.GetOwnerReportRecapLoadPlanAsync(
                criteria.OrganizationId,
                officeIdsCsv,
                periodStart.Value,
                criteria.PropertyId);
            if (loadPlan.UseNarrowRecapLoad && loadPlan.RecapLoadStartDate.HasValue && reportEndDate.HasValue)
            {
                loadCriteria = CloneJournalEntryRecapCriteriaWithDates(
                    criteria,
                    loadPlan.RecapLoadStartDate,
                    reportEndDate);
            }
        }

        var priorMonthClose = GetPriorMonthCloseDate(criteria.StartDate, criteria.EndDate);
        var bundle = await _journalEntryRepository.GetOwnerReportBundleDataAsync(loadCriteria, priorMonthClose, periodStart);
        var openingBalanceSheetCloseByOffice = ResolveOpeningBalanceSheetCloseDateByOffice(bundle.OwnerApLines);

        var recapLineSet = new RecapLineSet
        {
            AllLines = bundle.RecapLines,
            ActivityLines = FilterRecapLinesToReportMonthActivity(bundle.RecapLines, criteria)
        };

        if (officeIds.Count == 0)
        {
            return new OwnerReportLoadedData
            {
                RecapLineSet = recapLineSet,
                OfficeIds = officeIds
            };
        }

        var priorMonthAnchorByKey = new Dictionary<string, OwnerReportMonthlyAnchor>(StringComparer.OrdinalIgnoreCase);
        if (periodStart.HasValue)
        {
            var priorAnchors = await _accountingRepository.GetOwnerReportPriorMonthAnchorsAsync(
                criteria.OrganizationId,
                officeIdsCsv,
                periodStart.Value,
                criteria.PropertyId);
            foreach (var anchor in priorAnchors.Where(IsTrustedOwnerReportPriorMonthAnchor))
            {
                priorMonthAnchorByKey[GetPropertyReportKey(anchor.OfficeId, anchor.PropertyId)] = anchor;
            }
        }

        var properties = await LoadOwnerPropertyReportDataAsync(criteria);
        var startingBalanceByKey = BuildOwnerStartingBalanceByProperty(criteria, officeIds, bundle.OwnerApLines);
        var outstandingInvoices = (await _accountingRepository.GetOwnerInvoiceOutstandingByCriteriaAsync(criteria.OrganizationId, criteria.PropertyId, string.IsNullOrWhiteSpace(criteria.OfficeIds) ? null : criteria.OfficeIds, criteria.EndDate) ?? []).ToList();
        return new OwnerReportLoadedData
        {
            RecapLineSet = recapLineSet,
            Properties = properties,
            StartingBalanceByKey = startingBalanceByKey,
            OpeningBalanceSheetCloseByOffice = openingBalanceSheetCloseByOffice,
            OwnerApLines = bundle.OwnerApLines,
            OfficeIds = officeIds,
            OutstandingInvoices = outstandingInvoices,
            PriorMonthAnchorByKey = priorMonthAnchorByKey
        };
    }
}
