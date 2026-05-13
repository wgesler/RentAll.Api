using RentAll.Api.Dtos.Properties.PropertyPhotos;

namespace RentAll.Api.Dtos.Common;

public class PublicPropertyListingResponseDto
{
    public PropertyResponseDto Property { get; set; } = null!;
    public List<PropertyPhotoResponseDto> Photos { get; set; } = [];
}
