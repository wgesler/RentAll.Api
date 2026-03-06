using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class AzureBlobStorageService : IFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly StorageSettings _storageSettings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        StorageSettings storageSettings,
        AppSettings appSettings,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _storageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Images
    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType)
    {
        var fileBytes = DecodeBase64(fileContent);

        // Create stream and ensure it stays alive during async upload
        using var stream = new MemoryStream(fileBytes);
        var result = await SaveImageAsync(organizationId, officeName, stream, fileName, contentType, imageType);
        return result;
    }

    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType)
    {
        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
        var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        // Validate content type
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Content type must be an image type");

        try
        {
            var containerName = (string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container).ToLowerInvariant();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            var typeString = imageType.ToString().ToLowerInvariant();

            // Keep versioned logos (new URL each time). Folders: org/office/Photos|Receipts|Logos/file
            var uniqueFileName = $"{fileNameOnly}-{Guid.NewGuid()}{fileExtension}";
            var blobPathPrefix = GetBlobPathPrefix(organizationId, officeName);
            var blobName = $"{blobPathPrefix}/{typeString}/{uniqueFileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Read stream into a fresh MemoryStream to avoid disposal issues during async upload
            MemoryStream uploadStream;
            if (fileStream is MemoryStream ms && ms.CanRead && ms.CanSeek)
            {
                // Reset position and create a new stream from the bytes to avoid disposal issues
                ms.Position = 0;
                var bytes = ms.ToArray();
                uploadStream = new MemoryStream(bytes, false); // writable: false since we won't modify it
            }
            else
            {
                // For other stream types, copy to a new memory stream
                if (fileStream.CanSeek) fileStream.Position = 0;
                uploadStream = new MemoryStream();
                await fileStream.CopyToAsync(uploadStream);
                uploadStream.Position = 0; // Reset for reading
            }

            try
            {
                // Upload using the memory stream - it will be disposed after upload completes
                await blobClient.UploadAsync(uploadStream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } });
            }
            finally
            {
                // Ensure stream is disposed after upload
                await uploadStream.DisposeAsync();
            }

            return BuildUrl(containerName, blobName, blobClient.Uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var parsed = ParseStoragePath(filePath);
            if (parsed == null)
                return false;

            var (containerName, blobName) = parsed.Value;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from Azure Blob Storage: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<FileDetails?> GetImageDetailsAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            var parsed = ParseStoragePath(filePath);
            if (parsed == null)
                return null;

            var (containerName, blobName) = parsed.Value;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return null;

            var downloadResult = await blobClient.DownloadContentAsync();
            var fileBytes = downloadResult.Value.Content.ToArray();
            var base64Content = Convert.ToBase64String(fileBytes);

            var properties = await blobClient.GetPropertiesAsync();
            var ct = properties.Value.ContentType ?? "application/octet-stream";

            return new FileDetails
            {
                FileName = Path.GetFileName(blobName),
                ContentType = ct,
                File = base64Content,
                DataUrl = $"data:{ct};base64,{base64Content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading logo from Azure Blob Storage: {FilePath}", filePath);
            return null;
        }
    }
    #endregion

    #region Documents
    public async Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType)
    {
        var fileBytes = DecodeBase64(fileContent);

        // Create stream and ensure it stays alive during async upload
        using var stream = new MemoryStream(fileBytes);
        var result = await SaveDocumentAsync(organizationId, officeName, stream, fileName, contentType, documentType);
        return result;
    }

    public async Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType)
    {
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg", ".png", ".txt" };
        var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        var allowedContentTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/jpeg",
            "image/png",
            "text/plain"
        };
        if (!allowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid content type. Allowed types: {string.Join(", ", allowedContentTypes)}");

        try
        {
            var containerName = BuildContainerName();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var uniqueFileName = $"{fileNameOnly}-{Guid.NewGuid()}{fileExtension}";
            var blobPathPrefix = GetBlobPathPrefix(organizationId, officeName);
            var blobName = $"{blobPathPrefix}/documents/{uniqueFileName}";
            var blobClient = containerClient.GetBlobClient(blobName);


            // Read stream into a fresh MemoryStream to avoid disposal issues during async upload
            MemoryStream uploadStream;
            if (fileStream is MemoryStream ms && ms.CanRead && ms.CanSeek)
            {
                // Reset position and create a new stream from the bytes to avoid disposal issues
                ms.Position = 0;
                var bytes = ms.ToArray();
                uploadStream = new MemoryStream(bytes, false); // writable: false since we won't modify it
            }
            else
            {
                // For other stream types, copy to a new memory stream
                if (fileStream.CanSeek) fileStream.Position = 0;
                uploadStream = new MemoryStream();
                await fileStream.CopyToAsync(uploadStream);
                uploadStream.Position = 0; // Reset for reading
            }

            try
            {
                // Upload using the memory stream - it will be disposed after upload completes
                await blobClient.UploadAsync(uploadStream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } });
            }
            finally
            {
                // Ensure stream is disposed after upload
                await uploadStream.DisposeAsync();
            }

            return BuildUrl(containerName, blobName, blobClient.Uri);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var parsed = ParseStoragePath(filePath);
            if (parsed == null)
                return false;

            var (containerName, blobName) = parsed.Value;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document from Azure Blob Storage: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            var parsed = ParseStoragePath(filePath);
            if (parsed == null)
                return null;

            var (containerName, blobName) = parsed.Value;
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return null;

            var downloadResult = await blobClient.DownloadContentAsync();
            var fileBytes = downloadResult.Value.Content.ToArray();
            var base64Content = Convert.ToBase64String(fileBytes);

            var properties = await blobClient.GetPropertiesAsync();
            var ct = properties.Value.ContentType ?? "application/octet-stream";

            return new FileDetails
            {
                FileName = Path.GetFileName(blobName),
                ContentType = ct,
                File = base64Content,
                DataUrl = $"data:{ct};base64,{base64Content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document from Azure Blob Storage: {FilePath}", filePath);
            return null;
        }
    }
    #endregion

    #region Private Helpers
    private string BuildContainerName()
    {
        var env = (string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container).ToLowerInvariant();
        return SanitizeContainerSegment(env);
    }

    private string GetBlobPathPrefix(Guid organizationId, string? officeName)
    {
        var orgSegment = $"{organizationId:N}".ToLowerInvariant();
        var officeSegment = !string.IsNullOrWhiteSpace(officeName) ? officeName.Trim().ToLower() : "global";
        return $"{orgSegment}/{officeSegment}";
    }

    private static string SanitizeContainerSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "development";
        var normalized = value.ToLowerInvariant().Trim();
        var allowed = new char[normalized.Length];
        for (int i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            allowed[i] = (char.IsLetterOrDigit(c) || c == '-') ? c : '-';
        }
        var result = new string(allowed).Trim('-');
        return string.IsNullOrEmpty(result) ? "development" : result.Length <= 63 ? result : result.Substring(0, 63);
    }

    private static (string containerName, string blobName)? ParseStoragePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;
        var path = Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && uri.IsAbsoluteUri
            ? uri.AbsolutePath
            : filePath;
        path = path.TrimStart('/').TrimEnd('/');
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        return (parts[0], string.Join("/", parts.Skip(1)));
    }

    private static string BuildUrl(string containerName, string blobName, Uri fallbackUri, string? baseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
            return $"{baseUrl.TrimEnd('/')}/{containerName}/{blobName}";

        return fallbackUri.ToString();
    }

    private string BuildUrl(string containerName, string blobName, Uri fallbackUri)
        => BuildUrl(containerName, blobName, fallbackUri, _storageSettings.AzureBlobBaseUrl);

    private static byte[] DecodeBase64(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
            throw new ArgumentException("File content is empty.");

        // Handle: data:<mime>;base64,<data>
        if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = fileContent.IndexOf(',');
            if (comma < 0 || comma == fileContent.Length - 1)
                throw new ArgumentException("Invalid data URL format.");

            var base64Data = fileContent[(comma + 1)..];
            return Convert.FromBase64String(base64Data);
        }

        // Assume raw base64
        return Convert.FromBase64String(fileContent);
    }
    #endregion
}
