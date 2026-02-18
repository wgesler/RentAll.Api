using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Services;

public interface ICalendarService
{
	string GeneratePropertyCalendarToken(Guid propertyId, Guid organizationId);
	bool IsValidPropertyCalendarToken(Guid propertyId, Guid organizationId, string token);
	string BuildPropertyCalendar(Guid propertyId, IEnumerable<Reservation> reservations, DateTimeOffset generatedOnUtc);
}
