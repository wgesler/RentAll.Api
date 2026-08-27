using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoImportCreatedResponseDto
{
    public Guid ImportId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = PropertyPhotoImportStatus.Pending.ToString();
    public int PhotoCount { get; set; }
}
