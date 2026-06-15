namespace RentAll.Api.Dtos.Organizations.Features;

public class FeatureResponseDto
{
    public int FeatureId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int FeatureTypeId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureTypeDescription { get; set; } = string.Empty;
    public bool HasAccess { get; set; }

    public FeatureResponseDto(Feature feature)
    {
        FeatureId = feature.FeatureId;
        OrganizationId = feature.OrganizationId;
        OfficeId = feature.OfficeId;
        OfficeName = feature.OfficeName;
        FeatureTypeId = (int)feature.FeatureTypeId;
        FeatureCode = feature.FeatureCode;
        FeatureTypeDescription = feature.FeatureTypeDescription;
        HasAccess = feature.HasAccess;
    }
}
