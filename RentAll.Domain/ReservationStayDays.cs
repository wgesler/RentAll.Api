using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain;

/// <summary>
/// Stay day counts for the reservation form and Accounting.Billed matchup rows.
/// Matches RentAll.Ui UtilityService.getCalendarDaySpanBetweenDates and
/// ReservationComponent.getDisplayedStayDaysFromDates (not invoice CalculateNumberOfDays).
/// </summary>
public static class ReservationStayDays
{
    public static int? GetCalendarDaySpanBetweenDates(DateOnly fromDate, DateOnly toDate)
    {
        var days = toDate.DayNumber - fromDate.DayNumber;
        return days > 0 ? days : null;
    }

    public static int GetNumberOfStayDays(DateOnly arrival, DateOnly departure, BillingType billingType)
        => CountMatchupStayDays(arrival, departure, billingType);

    /// <summary>
    /// Single stay-day rule for TotalNumberOfDays (reservation form). Billed DaysStayed/DaysBilled use <see cref="InvoiceBillingDays"/>.
    /// </summary>
    public static int CountMatchupStayDays(DateOnly arrival, DateOnly departure, BillingType billingType)
    {
        var span = GetCalendarDaySpanBetweenDates(arrival, departure);
        if (!span.HasValue)
            return 0;

        return billingType == BillingType.Nightly ? span.Value : span.Value + 1;
    }

    public static int GetNumberOfStayDays(Reservation reservation)
    {
        var arrival = reservation.BillingStartDate ?? reservation.ArrivalDate;
        var departure = reservation.BillingEndDate ?? reservation.DepartureDate;
        return GetNumberOfStayDays(arrival, departure, reservation.BillingType);
    }

    public static DateOnly LastDayOfMonth(DateOnly date)
        => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    /// <summary>
    /// Billing stay start (billing start date or arrival), clipped to <paramref name="invoiceStartDate"/> when the stay begins before it.
    /// </summary>
    public static DateOnly ResolveDaysStayedStartDate(DateOnly billingStayStartDate, DateOnly invoiceStartDate)
        => billingStayStartDate < invoiceStartDate ? invoiceStartDate : billingStayStartDate;

    /// <summary>
    /// Billed-table DaysSinceStart: billing stay start (clipped to invoice start when earlier),
    /// through the last day of the calendar month containing <paramref name="asOfDate"/> — not clipped to early
    /// departure when the guest leaves before month-end.
    /// </summary>
    public static int GetDaysSinceStartForBillingMatchup(
        DateOnly billingStayStartDate,
        DateOnly billingStayEndDate,
        BillingType billingType,
        DateOnly invoiceStartDate,
        DateOnly asOfDate)
    {
        var endOfThisMonth = LastDayOfMonth(asOfDate);
        var effectiveStart = ResolveDaysStayedStartDate(billingStayStartDate, invoiceStartDate);

        if (billingStayEndDate < effectiveStart || endOfThisMonth < effectiveStart)
            return 0;

        return CountMatchupStayDays(effectiveStart, endOfThisMonth, billingType);
    }

    /// <summary>
    /// Billed-table DaysStayed: same start as <see cref="GetDaysSinceStartForBillingMatchup"/>, but end is
    /// min(departure, end of the calendar month containing <paramref name="asOfDate"/>).
    /// </summary>
    public static int GetDaysStayedForBillingMatchup(
        DateOnly billingStayStartDate,
        DateOnly billingStayEndDate,
        BillingType billingType,
        DateOnly invoiceStartDate,
        DateOnly asOfDate)
    {
        var endOfThisMonth = LastDayOfMonth(asOfDate);
        var effectiveEnd = billingStayEndDate <= endOfThisMonth ? billingStayEndDate : endOfThisMonth;
        var effectiveStart = ResolveDaysStayedStartDate(billingStayStartDate, invoiceStartDate);

        if (effectiveEnd < effectiveStart)
            return 0;

        return CountMatchupStayDays(effectiveStart, effectiveEnd, billingType);
    }
}
