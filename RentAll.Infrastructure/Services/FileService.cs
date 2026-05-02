using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;

namespace RentAll.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly string _wwwRootPath;
    private readonly AppSettings _appSettings;
    private readonly ImageUploadSettings _imageUploadSettings;
    private readonly ILogger<FileService> _logger;

    public FileService(
        string wwwRootPath,
        AppSettings appSettings,
        IOptions<ImageUploadSettings> imageUploadOptions,
        ILogger<FileService> logger)
    {
        _wwwRootPath = wwwRootPath ?? throw new ArgumentNullException(nameof(wwwRootPath));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _imageUploadSettings = imageUploadOptions?.Value ?? throw new ArgumentNullException(nameof(imageUploadOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Images
    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType)
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

        ImageUploadLimits.ThrowIfExceedsMaxBytes(fileBytes.Length, _imageUploadSettings);
        var buffer = new MemoryStream(fileBytes);
        var (streamToSave, effectiveExtension, _) = await ImagePersistencePreparer.PrepareForSaveAsync(
            buffer, fileName, contentType, _imageUploadSettings).ConfigureAwait(false);
        try
        {
            return await SavePreparedImageToDiskAsync(organizationId, officeName, streamToSave, fileName, effectiveExtension, imageType).ConfigureAwait(false);
        }
        finally
        {
            await streamToSave.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType)
    {
        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".heic", ".heif" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");

        // Validate content type (images or PDF)
        var allowedContentType = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!allowedContentType)
            throw new ArgumentException("Content type must be an image type or application/pdf.");

        var buffer = await ImageUploadLimits.ReadImageStreamWithSizeCapAsync(fileStream, _imageUploadSettings).ConfigureAwait(false);
        var (streamToSave, effectiveExtension, _) = await ImagePersistencePreparer.PrepareForSaveAsync(
            buffer, fileName, contentType, _imageUploadSettings).ConfigureAwait(false);
        try
        {
            return await SavePreparedImageToDiskAsync(organizationId, officeName, streamToSave, fileName, effectiveExtension, imageType).ConfigureAwait(false);
        }
        finally
        {
            await streamToSave.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<string> SavePreparedImageToDiskAsync(
        Guid organizationId,
        string? officeName,
        Stream streamToSave,
        string originalFileName,
        string effectiveExtension,
        ImageType imageType)
    {
        var fileNameOnly = Path.GetFileNameWithoutExtension(originalFileName);
        var containerPath = GetContainerPath(organizationId, officeName);
        var typeString = imageType.ToString().ToLowerInvariant();
        var includeTypeFolder = ShouldIncludeImageTypeFolder(officeName, imageType);
        var uploadsPath = includeTypeFolder
            ? Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), typeString)
            : Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(uploadsPath);

        var uniqueFileName = $"{fileNameOnly}-{Guid.NewGuid()}{effectiveExtension}";
        var fullPath = Path.Combine(uploadsPath, uniqueFileName);

        await using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
        {
            await streamToSave.CopyToAsync(fileStreamOut).ConfigureAwait(false);
        }

        return includeTypeFolder
            ? "/" + containerPath + "/" + typeString + "/" + uniqueFileName
            : "/" + containerPath + "/" + uniqueFileName;
    }

    public Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        var containerPath = GetContainerPath(organizationId, officeName);
        var typeString = imageType.ToString().ToLowerInvariant();
        var includeTypeFolder = ShouldIncludeImageTypeFolder(officeName, imageType);
        var expectedPathPrefix = includeTypeFolder
            ? $"/{containerPath}/{typeString}/"
            : $"/{containerPath}/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return Task.FromResult(false);
        }

        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
            return Task.FromResult(false);

        var fullPath = includeTypeFolder
            ? Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), typeString, fileName)
            : Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), fileName);

        try
        {
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

    public async Task<FileDetails?> GetImageDetailsAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        var containerPath = GetContainerPath(organizationId, officeName);
        var typeString = imageType.ToString().ToLowerInvariant();
        var includeTypeFolder = ShouldIncludeImageTypeFolder(officeName, imageType);
        var expectedPathPrefix = includeTypeFolder
            ? $"/{containerPath}/{typeString}/"
            : $"/{containerPath}/";
        if (!filePath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to read file outside allowed directory: {FilePath} for organization {OrganizationId}, office {officeName}", filePath, organizationId, officeName);
            return null;
        }

        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(fileName))
            return null;

        var fullPath = includeTypeFolder
            ? Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), typeString, fileName)
            : Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), fileName);

        try
        {
            if (!File.Exists(fullPath))
                return null;

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var base64Content = Convert.ToBase64String(fileBytes);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
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
            _logger.LogError(ex, "Error reading image file: {FilePath}", filePath);
            return null;
        }
    }

    #endregion

    #region Documents
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
        var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
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
        var uniqueFileName = $"{fileNameOnly}-{Guid.NewGuid()}{fileExtension}";
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
            var fullPath = Path.Combine(_wwwRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
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
            var fullPath = Path.Combine(_wwwRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
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

    private string GetContainerPath(Guid organizationId, string? officeName)
    {
        var envName = (string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container).ToLowerInvariant();
        var orgSegment = $"{organizationId:N}".ToLowerInvariant();
        var officeSegment = !string.IsNullOrWhiteSpace(officeName) ? officeName.Trim().ToLower() : "global";
        return $"{envName}/{orgSegment}/{officeSegment}";
    }

    private static bool ShouldIncludeImageTypeFolder(string? officeName, ImageType imageType)
    {
        if (imageType != ImageType.Photos)
            return true;

        if (string.IsNullOrWhiteSpace(officeName))
            return true;

        return !officeName.Contains("/listings/", StringComparison.OrdinalIgnoreCase)
            && !officeName.Contains("/inspection/", StringComparison.OrdinalIgnoreCase);
    }

}
