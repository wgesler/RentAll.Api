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
        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
        var invoiceStart = accountingOffice != null
            ? AccountingOfficePeriodBoundary.GetInvoiceStart(accountingOffice)
            : BilledMatchupInvoiceStart.InvoiceStart;
        var scanThroughDate = referenceDate <= billingStayEndDate ? referenceDate : billingStayEndDate;
        var throughMonth = FirstDayOfMonth(scanThroughDate);

        var rows = new List<Billed>();
        foreach (var billingMonth in EnumerateBillableMonths(reservation, throughMonth, invoiceStart))
        {
            var monthEnd = LastDayOfMonth(billingMonth);
            var (periodStart, periodEnd) = ResolveBillingPeriodForMonth(reservation, billingMonth);
            var daysStayed = CalculateBillableDaysInPeriod(
                reservation,
                periodStart,
                periodEnd,
                billingStayEndDate);
            var daysBilled = RentalFeeLineParser.CalculateDaysBilledForBillingPeriod(
                invoices,
                reservation,
                invoiceStart,
                periodStart,
                periodEnd,
                billingStayEndDate);
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
    {
        if (periodEnd < periodStart)
            return 0;

        var isDepartureMonthYear = InvoiceBillingDays.IsDepartureMonthYear(periodEnd, billingDepartureDate);
        var isLastDayOfMonth = InvoiceBillingDays.IsLastDayOfMonth(periodEnd);
        return InvoiceBillingDays.CalculateNumberOfDays(
            periodStart,
            periodEnd,
            reservation.BillingType,
            isDepartureMonthYear,
            isLastDayOfMonth);
    }
}
