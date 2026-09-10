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

    public async Task<CloseOwnerStatementMonthResult> CloseOwnerStatementMonthAsync(
        Guid organizationId,
        DateOnly? startDate,
        DateOnly endDate,
        IReadOnlyList<OwnerStatementMonthCloseLine> lines,
        Guid currentUser)
    {
        if (lines == null || lines.Count == 0)
            throw new Exception("At least one owner statement line is required.");

        if (endDate == default)
            throw new Exception("End date is required to close an owner statement month.");

        var periodStart = startDate ?? new DateOnly(endDate.Year, endDate.Month, 1);
        var periodMonth = new DateOnly(endDate.Year, endDate.Month, 1);
        var officeIds = string.Join(",", lines.Select(line => line.OfficeId).Where(officeId => officeId > 0).Distinct());

        var criteria = new JournalEntryRecapGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds,
            StartDate = periodStart,
            EndDate = endDate,
            IncludeUnposted = true
        };

        var cashReport = await GetOwnerCashReportAsync(criteria);
        var cashRowByPropertyKey = (cashReport.Rows ?? [])
            .ToDictionary(row => GetPropertyReportKey(row.OfficeId, row.PropertyId), StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (line.PropertyId == Guid.Empty || line.OfficeId <= 0)
                continue;

            if (!cashRowByPropertyKey.TryGetValue(GetPropertyReportKey(line.OfficeId, line.PropertyId), out var cashRow))
                throw new Exception($"Owner cash report row not found for property {line.PropertyCode}.");

            await _accountingRepository.UpsertOwnerReportMonthlyAnchorAsync(new OwnerReportMonthlyAnchor
            {
                OrganizationId = organizationId,
                OfficeId = line.OfficeId,
                PropertyId = line.PropertyId,
                PeriodMonth = periodMonth,
                PeriodStartDate = periodStart,
                PeriodEndDate = endDate,
                StartingBalance = cashRow.StartingBalance,
                ReceivedIncome = cashRow.ReceivedIncome,
                OwnerExpenses = cashRow.OwnerExpenses,
                OwnerPayment = cashRow.OwnerPayment,
                OwnerPaymentPaid = cashRow.OwnerPaymentPaid,
                EndingBalance = cashRow.EndingBalance,
                WorkingCapital = cashRow.WorkingCapital,
                AnchorStatusId = OwnerReportAnchorStatus.Closed,
                CalculationVersion = OwnerReportAnchorCalculation.CurrentVersion,
                IsDirty = false
            }, currentUser);
        }

        return await _accountingManager.CloseOwnerStatementMonthAsync(
            organizationId,
            endDate,
            lines,
            currentUser);
    }
}
