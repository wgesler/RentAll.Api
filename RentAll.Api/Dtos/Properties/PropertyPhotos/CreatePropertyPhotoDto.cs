using RentAll.Domain.Models.Common;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class CreatePropertyPhotoDto
{
    public int Order { get; set; }
    public FileDetails? FileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (Order < 0)
            return (false, "Order must be greater than or equal to zero");

        if (FileDetails == null || string.IsNullOrWhiteSpace(FileDetails.File))
            return (false, "File is required");

        if (string.IsNullOrWhiteSpace(FileDetails.FileName))
            return (false, "File name is required");

        if (string.IsNullOrWhiteSpace(FileDetails.ContentType))
            return (false, "Content type is required");

        return (true, null);
    }

    public PropertyPhoto ToModel(Guid propertyId)
    {
        return new PropertyPhoto
        {
            PropertyId = propertyId,
            Order = Order,
            PhotoPath = string.Empty
        };
    }
}
