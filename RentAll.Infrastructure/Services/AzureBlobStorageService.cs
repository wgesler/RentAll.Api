using Azure;
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
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        StorageSettings storageSettings,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _storageSettings = storageSettings ?? throw new ArgumentNullException(nameof(storageSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SaveLogoAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, EntityType entityType)
    {
        var fileBytes = DecodeBase64(fileContent);

        // Create stream and ensure it stays alive during async upload
        using var stream = new MemoryStream(fileBytes);
        var result = await SaveLogoAsync(organizationId, officeId, stream, fileName, contentType, entityType);
        return result;
    }

    public async Task<string> SaveLogoAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, EntityType entityType)
    {
        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        // Validate content type
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Content type must be an image type");

        try
        {
            var containerName = BuildContainerName(organizationId, officeId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            var typeString = entityType.ToString().ToLowerInvariant();

            // Keep versioned logos (new URL each time)
            var uniqueFileName = $"{typeString}-{Guid.NewGuid()}{fileExtension}";
            var blobName = $"logos/{uniqueFileName}";
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
            _logger.LogError(ex, "Error saving logo to Azure Blob Storage: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteLogoAsync(Guid organizationId, int? officeId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var containerName = BuildContainerName(organizationId, officeId);
            var blobName = ExtractBlobName(filePath, containerName, "logos");
            if (string.IsNullOrEmpty(blobName))
                return false;

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting logo from Azure Blob Storage: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, int? officeId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            var containerName = BuildContainerName(organizationId, officeId);
            var blobName = ExtractBlobName(filePath, containerName, "logos");
            if (string.IsNullOrEmpty(blobName))
                return null;

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

    public async Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, DocumentType documentType)
    {
        var fileBytes = DecodeBase64(fileContent);

        // Create stream and ensure it stays alive during async upload
        using var stream = new MemoryStream(fileBytes);
        var result = await SaveDocumentAsync(organizationId, officeId, stream, fileName, contentType, documentType);
        return result;
    }

    public async Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, DocumentType documentType)
    {
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg", ".png", ".txt" };
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
            var containerName = BuildContainerName(organizationId, officeId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var documentTypeFolder = documentType.ToString().ToLowerInvariant();
            var uniqueFileName = $"{documentTypeFolder}-{Guid.NewGuid()}{fileExtension}";
            var blobName = $"documents/{documentTypeFolder}/{uniqueFileName}";
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

    public async Task<bool> DeleteDocumentAsync(Guid organizationId, int? officeId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var containerName = BuildContainerName(organizationId, officeId);
            var blobName = ExtractBlobNameFromDocumentPath(filePath, containerName);
            if (string.IsNullOrEmpty(blobName))
                return false;

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

    public async Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, int? officeId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            var containerName = BuildContainerName(organizationId, officeId);
            var blobName = ExtractBlobNameFromDocumentPath(filePath, containerName);
            if (string.IsNullOrEmpty(blobName))
                return null;

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

    private static string BuildContainerName(Guid organizationId, int? officeId)
        => (officeId.HasValue ? $"{organizationId}-{officeId.Value}" : $"{organizationId}-0").ToLowerInvariant();

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

    private string? ExtractBlobName(string filePath, string containerName, string folderName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var expectedPrefix = $"{folderName}/";

        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Exact container match: {containerName}/{folderName}/...
            if (pathParts.Length >= 3
                && pathParts[0].Equals(containerName, StringComparison.OrdinalIgnoreCase)
                && pathParts[1].Equals(folderName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Join("/", pathParts.Skip(1));
            }

            // Fallback: find folder name somewhere in path
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i].Equals(folderName, StringComparison.OrdinalIgnoreCase))
                    return string.Join("/", pathParts.Skip(i));
            }
        }

        var normalizedPath = filePath.TrimStart('/').TrimEnd('/');
        if (normalizedPath.StartsWith($"{containerName}/{folderName}/", StringComparison.OrdinalIgnoreCase))
            return normalizedPath.Substring(containerName.Length + 1);

        if (normalizedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return normalizedPath;

        return null;
    }

    private string? ExtractBlobNameFromDocumentPath(string filePath, string containerName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length >= 4
                && pathParts[0].Equals(containerName, StringComparison.OrdinalIgnoreCase)
                && pathParts[1].Equals("documents", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join("/", pathParts.Skip(1));
            }
        }

        var normalizedPath = filePath.TrimStart('/').TrimEnd('/');
        if (normalizedPath.StartsWith($"{containerName}/documents/", StringComparison.OrdinalIgnoreCase))
            return normalizedPath.Substring(containerName.Length + 1);

        if (normalizedPath.StartsWith("documents/", StringComparison.OrdinalIgnoreCase))
            return normalizedPath;

        return null;
    }
}
