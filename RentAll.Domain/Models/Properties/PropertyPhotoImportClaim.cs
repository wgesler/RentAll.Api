namespace RentAll.Domain.Models.Properties;

public class PropertyPhotoImportClaim
{
    public PropertyPhotoImportItem Item { get; set; } = new();
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
}
