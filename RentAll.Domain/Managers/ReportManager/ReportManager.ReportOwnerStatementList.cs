using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<OwnerStatementListReport> GetOwnerStatementListAsync(JournalEntryRecapGetCriteria criteria)
    {
        if (GetReportOfficeIds(criteria.OfficeIds).Count == 0)
            return new OwnerStatementListReport();

        var bundle = await _journalEntryRepository.GetOwnerStatementListDataAsync(criteria);
        return new OwnerStatementListReport
        {
            Rows = bundle.Rows ?? [],
            OutstandingInvoices = bundle.OutstandingInvoices ?? []
        };
    }
}
