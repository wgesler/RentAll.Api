using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Feature
{
    public int FeatureId { get; set; }
    public Guid OrganizationId { get; set; }
    public FeatureType FeatureTypeId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureTypeDescription { get; set; } = string.Empty;
    public bool HasAccess { get; set; }
}
