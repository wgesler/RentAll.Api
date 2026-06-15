namespace RentAll.Infrastructure.Entities.Organizations;

public class FeatureEntity
{
    public int FeatureId { get; set; }
    public Guid OrganizationId { get; set; }
    public int FeatureTypeId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureTypeDescription { get; set; } = string.Empty;
    public bool HasAccess { get; set; }
}
