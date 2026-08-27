using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Properties;

public class PropertyPhotoImport
{
    public Guid ImportId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public PropertyPhotoImportStatus Status { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? CompletedOn { get; set; }
}
