using RentAll.Domain;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class ReservationStayDaysTests
{
    [Theory]
    [InlineData(BillingType.Monthly, 7, 8)]
    [InlineData(BillingType.Daily, 7, 8)]
    [InlineData(BillingType.Nightly, 7, 7)]
    public void GetNumberOfStayDays_MatchesReservationFormFormula(BillingType billingType, int calendarSpan, int expectedDays)
    {
        var arrival = new DateOnly(2026, 9, 1);
        var departure = arrival.AddDays(calendarSpan);

        Assert.Equal(expectedDays, ReservationStayDays.GetNumberOfStayDays(arrival, departure, billingType));
    }

    [Fact]
    public void GetNumberOfStayDays_UsesBillingDatesWhenPresent()
    {
        var reservation = new Reservation
        {
            ArrivalDate = new DateOnly(2026, 1, 1),
            DepartureDate = new DateOnly(2026, 12, 31),
            BillingStartDate = new DateOnly(2026, 9, 1),
            BillingEndDate = new DateOnly(2026, 9, 22),
            BillingType = BillingType.Monthly
        };

        Assert.Equal(22, ReservationStayDays.GetNumberOfStayDays(reservation));
    }

    [Fact]
    public void GetCalendarDaySpanBetweenDates_ReturnsNullWhenNotAfterArrival()
    {
        var date = new DateOnly(2026, 9, 1);
        Assert.Null(ReservationStayDays.GetCalendarDaySpanBetweenDates(date, date));
        Assert.Null(ReservationStayDays.GetCalendarDaySpanBetweenDates(date, date.AddDays(-1)));
    }

    [Fact]
    public void ResolveDaysStayedStartDate_UsesInvoiceStartWhenBillingStartIsEarlier()
    {
        var billingStart = new DateOnly(2025, 4, 18);
        var invoiceStart = new DateOnly(2026, 6, 1);

        Assert.Equal(invoiceStart, ReservationStayDays.ResolveDaysStayedStartDate(billingStart, invoiceStart));
        Assert.Equal(billingStart, ReservationStayDays.ResolveDaysStayedStartDate(billingStart, new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void GetDaysSinceStartForBillingMatchup_UsesInvoiceStartAndEndOfCurrentMonth()
    {
        var stayStart = new DateOnly(2025, 4, 18);
        var stayEnd = new DateOnly(2026, 12, 31);
        var invoiceStart = new DateOnly(2026, 6, 1);
        var asOf = new DateOnly(2026, 9, 15);

        var daysSinceStart = ReservationStayDays.GetDaysSinceStartForBillingMatchup(
            stayStart,
            stayEnd,
            BillingType.Monthly,
            invoiceStart,
            asOf);

        Assert.Equal(122, daysSinceStart);
        Assert.NotEqual(ReservationStayDays.GetNumberOfStayDays(stayStart, stayEnd, BillingType.Monthly), daysSinceStart);
    }

    [Fact]
    public void GetDaysStayedForBillingMatchup_CapsAtDepartureWhenGuestLeavesMidMonth()
    {
        var stayStart = new DateOnly(2025, 4, 18);
        var stayEnd = new DateOnly(2026, 9, 15);
        var invoiceStart = new DateOnly(2026, 6, 1);
        var asOf = new DateOnly(2026, 9, 28);

        var daysSinceStart = ReservationStayDays.GetDaysSinceStartForBillingMatchup(
            stayStart,
            stayEnd,
            BillingType.Monthly,
            invoiceStart,
            asOf);
        var daysStayed = ReservationStayDays.GetDaysStayedForBillingMatchup(
            stayStart,
            stayEnd,
            BillingType.Monthly,
            invoiceStart,
            asOf);

        Assert.Equal(122, daysSinceStart);
        Assert.Equal(107, daysStayed);
        Assert.True(daysSinceStart > daysStayed);
    }
}
