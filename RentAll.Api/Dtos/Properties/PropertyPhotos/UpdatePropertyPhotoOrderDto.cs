namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class UpdatePropertyPhotoOrderDto
{
    public int PhotoId { get; set; }
    public int Order { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PhotoId <= 0)
            return (false, "PhotoId is required");

        if (Order < 0)
            return (false, "Order must be greater than or equal to zero");

        return (true, null);
    }
}
