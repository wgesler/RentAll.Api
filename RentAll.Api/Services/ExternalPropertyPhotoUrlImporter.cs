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
        => await DownloadAndSaveCoreAsync(organizationId, listingScope, url, ImageType.Photos, allowPdf: false);

    public async Task<(string? StoredPath, string? ErrorMessage)> DownloadAndSaveDocumentAsync(Guid organizationId, string? officeName, string url, ImageType imageType, bool allowPdf)
        => await DownloadAndSaveCoreAsync(organizationId, officeName, url, imageType, allowPdf);

    public async Task<(string? StoredPath, string? ErrorMessage)> DownloadAndSavePropertyAgreementAsync(Guid organizationId, string? officeName, string url)
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
                return (null, $"Unable to download file (HTTP {(int)response.StatusCode})");

            var contentType = ResolveContentType(response.Content.Headers.ContentType?.MediaType, uri);
            if (!IsAllowedContentType(contentType, allowPdf: true))
                return (null, "Url must point to an image or PDF");

            var fileName = ResolveFileName(uri, contentType);
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var storedPath = await _fileService.SavePropertyAgreementFileAsync(organizationId, officeName, stream, fileName, contentType).ConfigureAwait(false);
            return (storedPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading external property agreement from {FileUrl}", url);
            return (null, "Unable to download file");
        }
    }

    private async Task<(string? StoredPath, string? ErrorMessage)> DownloadAndSaveCoreAsync(Guid organizationId, string? scope, string url, ImageType imageType, bool allowPdf)
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
                return (null, $"Unable to download file (HTTP {(int)response.StatusCode})");

            var contentType = ResolveContentType(response.Content.Headers.ContentType?.MediaType, uri);
            if (!IsAllowedContentType(contentType, allowPdf))
                return (null, allowPdf ? "Url must point to an image or PDF" : "Url must point to an image");

            var fileName = ResolveFileName(uri, contentType);
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var storedPath = await _fileService.SaveImageAsync(organizationId, scope, stream, fileName, contentType, imageType).ConfigureAwait(false);
            return (storedPath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading external file from {FileUrl}", url);
            return (null, "Unable to download file");
        }
    }

    private static bool IsAllowedContentType(string contentType, bool allowPdf) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || (allowPdf && string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase));

    private static string ResolveContentType(string? mediaType, Uri uri)
    {
        var contentType = (mediaType ?? string.Empty).Trim();
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return contentType;

        return Path.GetExtension(uri.LocalPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            ".pdf" => "application/pdf",
            _ => contentType
        };
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
