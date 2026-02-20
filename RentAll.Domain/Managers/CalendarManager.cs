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

        var reservations = await _reservationRepository.GetByPropertyIdAsync(propertyId, organizationId);
        var calendarReservations = reservations
            .Where(r => r.IsActive)
            .Where(r => r.ReservationStatus != ReservationStatus.PreBooking)
            .Where(r => r.DepartureDate > DateTimeOffset.UtcNow.AddYears(-1))
            .ToList();

        return _calendarService.BuildPropertyCalendar(propertyId, calendarReservations, DateTimeOffset.UtcNow);
    }
}
