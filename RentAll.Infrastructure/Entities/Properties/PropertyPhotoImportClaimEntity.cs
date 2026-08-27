namespace RentAll.Infrastructure.Entities.Properties;

public class PropertyPhotoImportClaimEntity : PropertyPhotoImportItemEntity
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
}
