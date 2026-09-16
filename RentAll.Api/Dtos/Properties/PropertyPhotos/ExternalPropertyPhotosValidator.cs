namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public static class ExternalPropertyPhotosValidator
{
    public const int MaxPhotosPerRequest = 25;

    public static (bool IsValid, string? ErrorMessage) ValidatePhotos(
        IReadOnlyList<ExternalPropertyPhotoUrlItemDto>? photos,
        string fieldPrefix = "Photos",
        bool requireAtLeastOne = false)
    {
        if (photos == null || photos.Count == 0)
        {
            if (requireAtLeastOne)
                return (false, $"{fieldPrefix} must contain at least one item");

            return (true, null);
        }

        if (photos.Count > MaxPhotosPerRequest)
            return (false, $"{fieldPrefix} cannot exceed {MaxPhotosPerRequest} items per request");

        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < photos.Count; index++)
        {
            var (itemIsValid, itemError) = photos[index].IsValid();
            if (!itemIsValid)
                return (false, $"{fieldPrefix}[{index}]: {itemError}");

            var normalizedUrl = photos[index].Url.Trim();
            if (!seenUrls.Add(normalizedUrl))
                return (false, $"{fieldPrefix} contains duplicate Url values");
        }

        return (true, null);
    }
}
