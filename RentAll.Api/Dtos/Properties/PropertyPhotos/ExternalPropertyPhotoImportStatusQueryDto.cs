namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoImportStatusQueryDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
}
