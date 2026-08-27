namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoUrlItemDto
{
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return (false, "Url is required");

        if (!Uri.TryCreate(Url.Trim(), UriKind.Absolute, out var uri))
            return (false, "Url must be an absolute URL");

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return (false, "Url must use https");

        if (SortOrder < 0)
            return (false, "SortOrder must be greater than or equal to zero");

        return (true, null);
    }
}
