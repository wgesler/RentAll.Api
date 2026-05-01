using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Api.Dtos.Properties.Properties;

namespace RentAll.Api.Dtos.Common;

public class PublicPropertyListingResponseDto
{
    public PropertyResponseDto Property { get; set; } = null!;
    public List<PropertyPhotoResponseDto> Photos { get; set; } = [];
}
