namespace RentAll.Api.Dtos.Organizations.Features;

public class FeatureUpdateDto
{
    public int FeatureId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int FeatureTypeId { get; set; }
    public bool HasAccess { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (FeatureId <= 0)
            return (false, "Feature ID is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!Enum.IsDefined(typeof(FeatureType), FeatureTypeId))
            return (false, $"Invalid FeatureTypeId value: {FeatureTypeId}");

        return (true, null);
    }

    public Feature ToModel()
    {
        return new Feature
        {
            FeatureId = FeatureId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            FeatureTypeId = (FeatureType)FeatureTypeId,
            HasAccess = HasAccess
        };
    }
}
