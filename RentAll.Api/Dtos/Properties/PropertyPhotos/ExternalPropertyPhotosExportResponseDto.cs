namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotosExportResponseDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public List<ExternalPropertyPhotoUrlItemDto> Photos { get; set; } = [];
}
