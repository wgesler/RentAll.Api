using Microsoft.Extensions.Logging;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly string _wwwRootPath;
    private readonly string _environment;
    private readonly ILogger<FileService> _logger;

    public FileService(string wwwRootPath, AppSettings appSettings, ILogger<FileService> logger)
    {
        _wwwRootPath = wwwRootPath ?? throw new ArgumentNullException(nameof(wwwRootPath));
        _environment = string.IsNullOrWhiteSpace(appSettings?.Environment) ? "development" : appSettings.Environment.ToLowerInvariant();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetContainerPath(Guid organizationId, string? officeName)
        => $"{_environment}/{(!string.IsNullOrEmpty(officeName) ? $"org{organizationId}/office{officeName}" : $"org{organizationId}")}".ToLowerInvariant();

    #region Images
    public async Task<string> SavePhotoAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType)
    {
        // Handle base64 encoded content
        byte[] fileBytes;
        if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Data URL format: data:image/png;base64,...
            var base64Data = fileContent.Split(',')[1];
            fileBytes = Convert.FromBase64String(base64Data);
        }
        else
        {
            // Assume it's already base64
            fileBytes = Convert.FromBase64String(fileContent);
        }

        using var stream = new MemoryStream(fileBytes);
        return await SaveImageAsync(organizationId, officeName, stream, fileName, contentType, entityType, ImageType.Photos);
    }

    public async Task<string> SaveReceiptAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType)
    {
        // Handle base64 encoded content
        byte[] fileBytes;
        if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Data URL format: data:image/png;base64,...
            var base64Data = fileContent.Split(',')[1];
            fileBytes = Convert.FromBase64String(base64Data);
        }
        else
        {
            // Assume it's already base64
            fileBytes = Convert.FromBase64String(fileContent);
        }

        using var stream = new MemoryStream(fileBytes);
        return await SaveImageAsync(organizationId, officeName, stream, fileName, contentType, entityType, ImageType.Receipts);
    }

    public async Task<string> SaveLogoAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType)
    {
        // Handle base64 encoded content
        byte[] fileBytes;
        if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Data URL format: data:image/png;base64,...
            var base64Data = fileContent.Split(',')[1];
            fileBytes = Convert.FromBase64String(base64Data);
        }
        else
        {
            // Assume it's already base64
            fileBytes = Convert.FromBase64String(fileContent);
        }

        using var stream = new MemoryStream(fileBytes);
        return await SaveImageAsync(organizationId, officeName, stream, fileName, contentType, entityType, ImageType.Logos);
    }

    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, EntityType entityType, ImageType imageType)
    {
        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        // Validate content type
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Content type must be an image type");

        // Create directory if it doesn't exist - organized by environment and org/office
        var containerPath = GetContainerPath(organizationId, officeName);
        var uploadsPath = Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), "logos");
        Directory.CreateDirectory(uploadsPath);

        // Convert EntityType to lowercase string for filename
        var typeString = entityType.ToString().ToLowerInvariant();

        // Generate unique filename
        var uniqueFileName = $"{typeString}-{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file
        using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOut);
        }

        // Return relative path
        return $"/{containerPath}/{imageType.ToString()}/{uniqueFileName}";
    }

    public Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        // Security: Ensure path is within the organization/office's logos directory
        var containerPath = GetContainerPath(organizationId, officeName);
        var expectedPathPrefix = $"/{containerPath}/{imageType.ToString()}/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return Task.FromResult(false);
        }

        try
        {
            var fullPath = Path.Combine(
                _wwwRootPath,
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }
    #endregion

    #region Documents
    public async Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, string? officeName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // Security: Ensure path is within the organization/office's logos directory
        var containerPath = GetContainerPath(organizationId, officeName);
        var expectedPathPrefix = $"/{containerPath}/logos/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to read file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return null;
        }

        try
        {
            var fullPath = Path.Combine(
                _wwwRootPath,
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
                return null;

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var base64Content = Convert.ToBase64String(fileBytes);
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Determine content type from extension
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            return new FileDetails
            {
                FileName = fileName,
                ContentType = contentType,
                File = base64Content,
                DataUrl = $"data:{contentType};base64,{base64Content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading logo file: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType)
    {
        // Handle base64 encoded content
        byte[] fileBytes;
        if (fileContent.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Data URL format: data:application/pdf;base64,...
            var base64Data = fileContent.Split(',')[1];
            fileBytes = Convert.FromBase64String(base64Data);
        }
        else
        {
            // Assume it's already base64
            fileBytes = Convert.FromBase64String(fileContent);
        }

        using var stream = new MemoryStream(fileBytes);
        return await SaveDocumentAsync(organizationId, officeName, stream, fileName, contentType, documentType);
    }

    public async Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType)
    {
        // Validate file type
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg", ".png", ".txt" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        // Validate content type
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

        // Create directory if it doesn't exist - organized by environment and org/office
        var containerPath = GetContainerPath(organizationId, officeName);
        var documentTypeFolder = documentType.ToString().ToLowerInvariant();
        var uploadsPath = Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), "documents", documentTypeFolder);
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var uniqueFileName = $"{documentTypeFolder}-{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file
        using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOut);
        }

        // Return relative path
        return $"/{containerPath}/documents/{documentTypeFolder}/{uniqueFileName}";
    }

    public Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        // Security: Ensure path is within the organization/office's documents directory
        var containerPath = GetContainerPath(organizationId, officeName);
        var expectedPathPrefix = $"/{containerPath}/documents/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return Task.FromResult(false);
        }

        try
        {
            var fullPath = Path.Combine(
                _wwwRootPath,
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public async Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        // Security: Ensure path is within the organization/office's documents directory
        var containerPath = GetContainerPath(organizationId, officeName);
        var expectedPathPrefix = $"/{containerPath}/documents/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to read file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return null;
        }

        try
        {
            var fullPath = Path.Combine(
                _wwwRootPath,
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
                return null;

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var base64Content = Convert.ToBase64String(fileBytes);
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Determine content type from extension
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            return new FileDetails
            {
                FileName = fileName,
                ContentType = contentType,
                File = base64Content,
                DataUrl = $"data:{contentType};base64,{base64Content}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading document file: {FilePath}", filePath);
            return null;
        }
    }
    #endregion
}
