namespace RentAll.Domain.Interfaces.Managers;

public interface ICalendarManager
{
    string GeneratePropertyCalendarSubscriptionUrl(Guid propertyId, Guid organizationId, string baseUrl);
    Task<string?> BuildPropertyCalendarFeedAsync(Guid propertyId, Guid organizationId, string token);
}
