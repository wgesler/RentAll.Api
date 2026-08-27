using RentAll.Domain.Interfaces.Services;
using System.Net;

namespace RentAll.Api.Services;

public class ExternalPropertyPhotoUrlImporter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileService _fileService;
    private readonly ILogger<ExternalPropertyPhotoUrlImporter> _logger;

    public ExternalPropertyPhotoUrlImporter(IHttpClientFactory httpClientFactory, IFileService fileService, ILogger<ExternalPropertyPhotoUrlImporter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<(string? PhotoPath, string? ErrorMessage)> DownloadAndSaveAsync(Guid organizationId, string listingScope, string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return (null, "Url must be an absolute URL");

        if (!IsAllowedPhotoUrl(uri))
            return (null, "Url is not allowed");

        var client = _httpClientFactory.CreateClient(nameof(ExternalPropertyPhotoUrlImporter));
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return (null, $"Unable to download photo (HTTP {(int)response.StatusCode})");

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return (null, "Url must point to an image");

            var fileName = ResolveFileName(uri, contentType);
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var photoPath = await _fileService.SaveImageAsync(organizationId, listingScope, stream, fileName, contentType, ImageType.Photos).ConfigureAwait(false);
            return (photoPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading external property photo from {PhotoUrl}", url);
            return (null, "Unable to download photo");
        }
    }

    private static bool IsAllowedPhotoUrl(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (uri.IsLoopback)
            return false;

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!IPAddress.TryParse(uri.Host, out var ipAddress))
            return true;

        if (IPAddress.IsLoopback(ipAddress))
            return false;

        if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return true;

        var bytes = ipAddress.GetAddressBytes();
        if (bytes[0] == 10)
            return false;

        if (bytes[0] == 127)
            return false;

        if (bytes[0] == 169 && bytes[1] == 254)
            return false;

        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return false;

        if (bytes[0] == 192 && bytes[1] == 168)
            return false;

        return true;
    }

    private static string ResolveFileName(Uri uri, string contentType)
    {
        var fileName = Path.GetFileName(uri.LocalPath);
        if (!string.IsNullOrWhiteSpace(fileName) && fileName.Contains('.', StringComparison.Ordinal))
            return fileName;

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/heic" => ".heic",
            "image/heif" => ".heif",
            _ => ".jpg"
        };

        return $"photo{extension}";
    }
}
