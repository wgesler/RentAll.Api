namespace RentAll.Api.Dtos.Organizations.Features;

public class FeatureCreateDto
{
    public Guid OrganizationId { get; set; }
    public int FeatureTypeId { get; set; }
    public bool HasAccess { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (!Enum.IsDefined(typeof(FeatureType), FeatureTypeId))
            return (false, $"Invalid FeatureTypeId value: {FeatureTypeId}");

        return (true, null);
    }

    public Feature ToModel()
    {
        return new Feature
        {
            OrganizationId = OrganizationId,
            FeatureTypeId = (FeatureType)FeatureTypeId,
            HasAccess = HasAccess
        };
    }
}
