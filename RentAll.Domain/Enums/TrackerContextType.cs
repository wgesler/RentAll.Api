namespace RentAll.Domain.Enums;

public enum TrackerContextType
{
    Unknown = 0,
    ReservationArrival = 1,
    ReservationDeparture = 2,
    PropertyOnline = 3,
    PropertyOffline = 4,
    PropertyThirdPartyOnline = 5,
    PropertyThirdPartyOffline = 6,
    PropertyDirectOnline = 7,
    PropertyDirectOffline = 8
}
