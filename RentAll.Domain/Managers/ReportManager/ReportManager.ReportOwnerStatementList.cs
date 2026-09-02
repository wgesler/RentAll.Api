using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class ReportManager
{
    public async Task<IReadOnlyList<OwnerInvoiceOutstanding>> GetOwnerInvoiceOutstandingAsync(JournalEntryRecapGetCriteria criteria)
    {
        if (GetReportOfficeIds(criteria.OfficeIds).Count == 0)
            return [];

        return await _accountingRepository.GetOwnerInvoiceOutstandingByCriteriaAsync(
            criteria.OrganizationId,
            criteria.PropertyId,
            string.IsNullOrWhiteSpace(criteria.OfficeIds) ? null : criteria.OfficeIds,
            criteria.EndDate);
    }

    public async Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(JournalEntryRecapGetCriteria criteria, Guid currentUser)
    {
        if (GetReportOfficeIds(criteria.OfficeIds).Count == 0)
            throw new Exception("At least one office is required.");

        if (!criteria.EndDate.HasValue)
            throw new Exception("End date is required to close an owner statement month.");

        criteria.IncludeUnposted = true;
        if (!criteria.StartDate.HasValue)
            criteria.StartDate = new DateOnly(criteria.EndDate.Value.Year, criteria.EndDate.Value.Month, 1);

        var cashReport = await GetOwnerCashReportAsync(criteria);
        var rows = cashReport.Rows ?? [];

        return await _accountingManager.CloseOwnerStatementMonthAsync(
            criteria.OrganizationId,
            criteria.OfficeIds,
            criteria.EndDate.Value,
            rows,
            currentUser);
    }
}
