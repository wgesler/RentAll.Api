using RentAll.Domain;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public int SumGetChargesRentalDaysInMatchupWindow(
        Reservation reservation,
        DateOnly invoiceStart,
        DateOnly windowEnd,
        int rentalCostCodeId)
    {
        var billingStart = ResolveBillingArrivalDate(reservation);
        var windowStart = billingStart < invoiceStart ? invoiceStart : billingStart;
        if (windowEnd < windowStart)
            return 0;

        var billingDeparture = ResolveBillingDepartureDate(reservation);
        var total = 0;
        var monthCursor = new DateOnly(windowStart.Year, windowStart.Month, 1);

        while (monthCursor <= windowEnd)
        {
            var monthLast = ReservationStayDays.LastDayOfMonth(monthCursor);
            var periodStart = windowStart > monthCursor ? windowStart : monthCursor;
            var periodEnd = windowEnd < monthLast ? windowEnd : monthLast;

            if (periodStart <= periodEnd)
            {
                var lines = GetLedgerLinesByReservationIdAsync(reservation, periodStart, periodEnd, rentalCostCodeId);
                total += InvoiceBillingDays.SumRentalFeeDaysFromLedgerLines(
                    lines,
                    reservation,
                    periodStart,
                    periodEnd,
                    billingDeparture);
            }

            monthCursor = monthCursor.AddMonths(1);
        }

        return total;
    }

    private int ResolveRentalCostCodeIdForMatchup(Reservation reservation)
        => FURNISHED_EXPENSE_COST_CODE > 0 ? FURNISHED_EXPENSE_COST_CODE : 77;
}
