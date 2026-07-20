using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface ICalendarService
{
    string GeneratePropertyCalendarToken(Guid propertyId, Guid organizationId);
    bool IsValidPropertyCalendarToken(Guid propertyId, Guid organizationId, string token);
    string BuildPropertyCalendar(Guid propertyId, IEnumerable<ReservationList> reservations, DateTimeOffset generatedOnUtc);
    Task<IReadOnlyList<ImportedCalendarEvent>> ImportExternalCalendarAsync(string externalCalendarUrl, CancellationToken cancellationToken = default);
}
