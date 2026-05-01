using RentAll.Domain.Models.Common;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class PropertyPhotoResponseDto
{
    public int PhotoId { get; set; }
    public Guid PropertyId { get; set; }
    public int Order { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }

    public PropertyPhotoResponseDto(PropertyPhoto photo)
    {
        PhotoId = photo.PhotoId;
        PropertyId = photo.PropertyId;
        Order = photo.Order;
        PhotoPath = photo.PhotoPath;
    }
}
