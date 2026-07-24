using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private static DateOnly GetAccountingOfficeStartDate(AccountingOffice office)
        => AccountingOfficePeriodBoundary.GetStartMonth(office);

    private static DateOnly GetAccountingMonthStart(DateOnly date)
        => new(date.Year, date.Month, 1);

    private static bool IsOnOrAfterAccountingOfficeStart(AccountingOffice office, DateOnly date)
        => date >= GetAccountingOfficeStartDate(office);

    private static bool IsAccountingMonthOnOrAfterOfficeStart(AccountingOffice office, DateOnly processingDate)
        => GetAccountingMonthStart(processingDate) >= GetAccountingOfficeStartDate(office);

    private static Dictionary<int, AccountingOffice> ToAccountingOfficeLookup(IReadOnlyCollection<AccountingOffice> accountingOffices)
        => accountingOffices.ToDictionary(office => office.OfficeId);

    private static DateOnly? ClampPeriodicSyncStartDate(DateOnly? startDate, IReadOnlyCollection<AccountingOffice> accountingOffices)
    {
        if (!startDate.HasValue || accountingOffices.Count == 0)
            return startDate;

        var earliestOfficeStart = accountingOffices.Min(GetAccountingOfficeStartDate);
        return startDate.Value < earliestOfficeStart ? earliestOfficeStart : startDate;
    }

    private static (DateOnly? StartDate, DateOnly? EndDate) ClampPeriodicSyncDateRange(
        DateOnly? startDate,
        DateOnly? endDate,
        IReadOnlyCollection<AccountingOffice> accountingOffices)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return (startDate, endDate);

        var clampedStart = ClampPeriodicSyncStartDate(startDate ?? endDate, accountingOffices);
        var resolvedEnd = endDate ?? startDate;
        if (clampedStart.HasValue && resolvedEnd.HasValue && clampedStart.Value > resolvedEnd.Value)
            return (clampedStart, clampedStart);

        return (clampedStart, resolvedEnd);
    }

    private static bool ShouldProcessPeriodicTaskMonthForOffice(AccountingOffice? accountingOffice, DateOnly processingDate)
        => accountingOffice == null || IsAccountingMonthOnOrAfterOfficeStart(accountingOffice, processingDate);
}
