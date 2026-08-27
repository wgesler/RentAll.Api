namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoBatchResponseDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public List<PropertyPhotoResponseDto> Photos { get; set; } = [];
    public List<ExternalPropertyPhotoBatchErrorDto> Errors { get; set; } = [];
}

public class ExternalPropertyPhotoBatchErrorDto
{
    public int Index { get; set; }
    public string? Url { get; set; }
    public string Message { get; set; } = string.Empty;
}
