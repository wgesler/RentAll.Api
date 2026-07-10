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
    }

    public async Task<OwnerReportsBundle> GetOwnerReportsBundleAsync(JournalEntryRecapGetCriteria criteria)
    {
        var loaded = await LoadOwnerReportLoadedDataAsync(criteria);
        return new OwnerReportsBundle
        {
            Cash = BuildOwnerCashReport(loaded, criteria),
            Accrual = BuildOwnerAccrualReport(loaded, criteria)
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
        var recapLineSet = await LoadRecapLinesAsync(criteria, includePaymentInvoiceContext: true);
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
        var startingBalanceByKey = await LoadOwnerStartingBalanceByPropertyAsync(criteria, officeIds);
        return new OwnerReportLoadedData
        {
            RecapLineSet = recapLineSet,
            Properties = properties,
            StartingBalanceByKey = startingBalanceByKey,
            OfficeIds = officeIds
        };
    }
}
