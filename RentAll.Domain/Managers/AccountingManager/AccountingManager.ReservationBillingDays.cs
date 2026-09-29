using RentAll.Domain;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public IReadOnlyList<Billed> BuildBilledMatchupMonthlyRows(
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
        var invoiceStart = accountingOffice != null
            ? AccountingOfficePeriodBoundary.GetInvoiceStart(accountingOffice)
            : BilledMatchupInvoiceStart.InvoiceStart;
        var throughMonth = FirstDayOfMonth(reservation.DepartureDate);

        var rows = new List<Billed>();
        foreach (var billingMonth in EnumerateBillableMonthsForBilledGrid(reservation, throughMonth, invoiceStart))
        {
            var monthEnd = LastDayOfMonth(billingMonth);
            var (periodStart, periodEnd) = ResolveBilledGridPeriodForMonth(reservation, billingMonth);
            var daysStayed = CalculateBillableDaysInPeriod(
                reservation,
                periodStart,
                periodEnd,
                reservation.DepartureDate);
            var daysBilled = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
                invoices,
                reservation,
                invoiceStart,
                periodStart,
                periodEnd,
                reservation.DepartureDate);
            var rentalFeeLines = RentalFeeLineParser.CollectRentalFeeLineDescriptionsForBillingPeriod(
                invoices,
                invoiceStart,
                periodStart,
                periodEnd).ToList();

            rows.Add(new Billed
            {
                OrganizationId = organizationId,
                OfficeId = reservation.OfficeId,
                ReservationId = reservation.ReservationId,
                ReservationCode = (reservation.ReservationCode ?? string.Empty).Trim(),
                StartDate = billingStayStartDate,
                EndDate = billingStayEndDate,
                InvoiceStart = invoiceStart,
                BillingType = reservation.BillingType,
                MonthStart = billingMonth,
                MonthEnd = monthEnd,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DaysStayed = daysStayed,
                DaysBilled = daysBilled,
                RentalFeeLines = rentalFeeLines,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            });
        }

        return rows;
    }

    private static int CalculateBillableDaysInPeriod(
        Reservation reservation,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly billingDepartureDate)
        => InvoiceBillingDays.CalculateBillableDaysInPeriod(
            periodStart,
            periodEnd,
            reservation.BillingType,
            billingDepartureDate);
}
