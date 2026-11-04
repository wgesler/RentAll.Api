namespace RentAll.Domain.Enums;

public enum ReservationStatus
{
    PreBooking = 0,
    Confirmed = 1,
    CheckedIn = 2,
    MonthToMonth = 3,
    GaveNotice = 4,
    FirstRightOfRefusal = 5,
    EarlyMoveOut = 6,
    Maintenance = 7,
    OwnerBlocked = 8,
    ArrivalDeparture = 9,
    Offline = 10
}
