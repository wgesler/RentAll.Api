namespace RentAll.Domain.Models;

public static class BilledMatchupInvoiceStart
{
    /// <summary>Default invoice window start when no accounting office is available.</summary>
    public static DateOnly InvoiceStart => AccountingOfficePeriodBoundary.GetInvoiceStart(new AccountingOffice());
}
