namespace RentAll.Domain.Helpers;

public static class ReservationStayDays
{
    public static DateOnly LastDayOfMonth(DateOnly date)
        => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
}
