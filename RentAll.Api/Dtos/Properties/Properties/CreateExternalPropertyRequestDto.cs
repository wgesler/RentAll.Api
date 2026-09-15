namespace RentAll.Api.Dtos.Properties.Properties;

public class CreateExternalPropertyRequestDto
{
    public const int MaxPropertiesPerRequest = 50;

    public List<CreateExternalPropertyDto> Properties { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (Properties == null || Properties.Count == 0)
            return (false, "Properties must contain at least one item");

        if (Properties.Count > MaxPropertiesPerRequest)
            return (false, $"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

        for (var index = 0; index < Properties.Count; index++)
        {
            var (itemIsValid, itemError) = Properties[index].IsValid();
            if (!itemIsValid)
                return (false, $"Properties[{index}]: {itemError}");
        }

        var organizationIds = Properties
            .Select(property => property.OrganizationId)
            .Distinct()
            .ToList();
        if (organizationIds.Count > 1)
            return (false, "All properties must use the same OrganizationId");

        return (true, null);
    }
}
