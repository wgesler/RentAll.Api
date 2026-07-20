namespace RentAll.Domain.Models;

public static class AccountingOfficePeriodBoundary
{
    public static DateOnly GetStartMonth(AccountingOffice office)
        => new DateOnly(office.StartYear, office.StartMonth, 1);
}
