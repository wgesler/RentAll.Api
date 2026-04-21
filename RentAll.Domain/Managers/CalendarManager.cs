using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Domain.Managers;

public class CalendarManager : ICalendarManager
{
    private readonly ICalendarService _calendarService;
    private readonly IReservationRepository _reservationRepository;

    public CalendarManager(ICalendarService calendarService, IReservationRepository reservationRepository)
    {
        _calendarService = calendarService;
        _reservationRepository = reservationRepository;
    }

    public string GeneratePropertyCalendarSubscriptionUrl(Guid propertyId, Guid organizationId, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required", nameof(baseUrl));

        var token = _calendarService.GeneratePropertyCalendarToken(propertyId, organizationId);
        return $"{baseUrl}/api/common/calendar/property/{propertyId}.ics?organizationId={organizationId}&token={token}";
    }

    public async Task<string?> BuildPropertyCalendarFeedAsync(Guid propertyId, Guid organizationId, string token)
    {
        if (!_calendarService.IsValidPropertyCalendarToken(propertyId, organizationId, token))
            return null;

        // Use the DB "active list" for this property so the feed is not empty when the list-by-property
        // result set omits or does not map IsActive (defaults to false and filters out every row in memory).
        var reservations = await _reservationRepository.GetReservationActiveListByPropertyIdAsync(propertyId, organizationId);
        var calendarReservations = reservations
            .Where(r => r.ReservationStatus != ReservationStatus.PreBooking)
            .Where(r => r.DepartureDate > DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-1)))
            .ToList();

        return _calendarService.BuildPropertyCalendar(propertyId, calendarReservations, DateTimeOffset.UtcNow);
    }
}
