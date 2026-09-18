namespace RentAll.Api.Dtos.Properties.PropertyPhotos;

public class ExternalPropertyPhotoUrlItemDto
{
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Url))
            errors.Add("Url is required");
        else if (!Uri.TryCreate(Url.Trim(), UriKind.Absolute, out var uri))
            errors.Add("Url must be an absolute URL");
        else if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            errors.Add("Url must use https");
        if (SortOrder < 0)
            errors.Add("SortOrder must be greater than or equal to zero");
        return errors.Count == 0 ? (true, null) : (false, string.Join("\n", errors));
    }
}
