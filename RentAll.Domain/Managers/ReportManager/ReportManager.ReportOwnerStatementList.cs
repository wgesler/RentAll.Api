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

    public Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(
        Guid organizationId,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines,
        Guid currentUser)
    {
        if (lines == null || lines.Count == 0)
            throw new Exception("At least one owner statement line is required.");

        if (endDate == default)
            throw new Exception("End date is required to close an owner statement month.");

        return _accountingManager.CloseOwnerStatementMonthAsync(
            organizationId,
            endDate,
            lines,
            currentUser);
    }
}
