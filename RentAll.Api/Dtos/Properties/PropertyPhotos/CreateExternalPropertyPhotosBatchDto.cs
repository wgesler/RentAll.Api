using RentAll.Api.Dtos.Properties.Properties;

namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class CreateExternalPropertyPhotosBatchDto : ExternalPropertyKeyRequest
{
    public const int MaxPhotosPerRequest = 25;

    public List<ExternalPropertyPhotoUrlItemDto> Photos { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var (keysAreValid, keysError) = ValidateRequiredKeys();
        if (!keysAreValid)
            return (false, keysError);

        if (Photos == null || Photos.Count == 0)
            return (false, "Photos must contain at least one item");

        if (Photos.Count > MaxPhotosPerRequest)
            return (false, $"Photos cannot exceed {MaxPhotosPerRequest} items per request");

        for (var index = 0; index < Photos.Count; index++)
        {
            var (itemIsValid, itemError) = Photos[index].IsValid();
            if (!itemIsValid)
                return (false, $"Photos[{index}]: {itemError}");
        }

        return (true, null);
    }
}
