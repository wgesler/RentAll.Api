using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public Billed BuildBilledMatchupRow(
        Reservation reservation,
        Guid organizationId,
        Guid currentUser,
        IReadOnlyList<Invoice>? reservationInvoices = null,
        DateOnly? asOfDate = null,
        AccountingOffice? accountingOffice = null)
    {
        var billingStayStartDate = ResolveBillingArrivalDate(reservation);
        var billingStayEndDate = ResolveBillingDepartureDate(reservation);
        var invoices = reservationInvoices ?? [];
        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
        var invoiceStart = accountingOffice != null
            ? AccountingOfficePeriodBoundary.GetInvoiceStart(accountingOffice)
            : BilledMatchupInvoiceStart.InvoiceStart;
        var rentalCostCodeId = ResolveRentalCostCodeIdForMatchup(reservation);

        var endOfThisMonth = ReservationStayDays.LastDayOfMonth(referenceDate);
        var daysSinceStart = SumGetChargesRentalDaysInMatchupWindow(
            reservation,
            invoiceStart,
            endOfThisMonth,
            rentalCostCodeId);

        var effectiveStayEnd = billingStayEndDate <= endOfThisMonth ? billingStayEndDate : endOfThisMonth;
        var daysStayed = SumGetChargesRentalDaysInMatchupWindow(
            reservation,
            invoiceStart,
            effectiveStayEnd,
            rentalCostCodeId);

        var rentalFeeLines = RentalFeeLineParser.CollectRentalFeeLineDescriptions(invoices, invoiceStart).ToList();
        var daysBilled = RentalFeeLineParser.CalculateDaysBilledFromInvoices(
            invoices,
            reservation,
            invoiceStart,
            billingStayEndDate);

        return new Billed
        {
            OrganizationId = organizationId,
            OfficeId = reservation.OfficeId,
            ReservationId = reservation.ReservationId,
            ReservationCode = (reservation.ReservationCode ?? string.Empty).Trim(),
            StartDate = billingStayStartDate,
            EndDate = billingStayEndDate,
            InvoiceStart = invoiceStart,
            BillingType = reservation.BillingType,
            TotalNumberOfDays = ReservationStayDays.GetNumberOfStayDays(reservation),
            DaysSinceStart = daysSinceStart,
            DaysStayed = daysStayed,
            DaysBilled = daysBilled,
            RentalFeeLines = rentalFeeLines,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
    }
}
