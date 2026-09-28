namespace RentAll.Domain.Models;

public static class AccountingOfficePeriodBoundary
{
    public static DateOnly GetStartMonth(AccountingOffice office)
        => new DateOnly(office.StartYear, office.StartMonth, 1);

    public static DateOnly GetInvoiceStart(AccountingOffice office)
        => new DateOnly(office.InvoiceStartYear, office.InvoiceStartMonth, 1);
}
