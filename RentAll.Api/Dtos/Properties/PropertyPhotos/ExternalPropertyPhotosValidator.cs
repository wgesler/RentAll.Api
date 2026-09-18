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

        var errors = new List<string>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < photos.Count; index++)
        {
            var (itemIsValid, itemError) = photos[index].IsValid();
            if (!itemIsValid)
                errors.AddRange((itemError ?? "Invalid request data").Split('\n').Select(line => $"{fieldPrefix}[{index}]: {line}"));

            var normalizedUrl = photos[index].Url?.Trim() ?? string.Empty;
            if (normalizedUrl.Length > 0 && !seenUrls.Add(normalizedUrl))
                errors.Add($"{fieldPrefix}[{index}]: Url is a duplicate");
        }

        return errors.Count == 0 ? (true, null) : (false, string.Join("\n", errors));
    }
}
