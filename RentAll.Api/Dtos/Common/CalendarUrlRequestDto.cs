namespace RentAll.Api.Dtos.Common;

public class CalendarUrlRequestDto
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Token { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid(Guid routePropertyId)
    {
        // Route id is the canonical property reference for this endpoint.
        if (routePropertyId == Guid.Empty)
            return (false, "Property ID is required");

        if (PropertyId != Guid.Empty && PropertyId != routePropertyId)
            return (false, "Property ID in query does not match route property ID");

        PropertyId = routePropertyId;

        if (OrganizationId == Guid.Empty)
            return (false, "Organization ID is required");

        if (string.IsNullOrWhiteSpace(Token))
            return (false, "Token is required");

        return (true, null);
    }
}
