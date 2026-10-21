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
    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType, string? propertyCode = null)
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
            buffer, fileName, contentType, _imageUploadSettings, imageType).ConfigureAwait(false);
        try
        {
            return await SavePreparedImageToDiskAsync(organizationId, officeName, streamToSave, fileName, effectiveExtension, imageType, propertyCode).ConfigureAwait(false);
        }
        finally
        {
            await streamToSave.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType, string? propertyCode = null)
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
            buffer, fileName, contentType, _imageUploadSettings, imageType).ConfigureAwait(false);
        try
        {
            return await SavePreparedImageToDiskAsync(organizationId, officeName, streamToSave, fileName, effectiveExtension, imageType, propertyCode).ConfigureAwait(false);
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
        ImageType imageType,
        string? propertyCode = null)
    {
        var fileNameOnly = Path.GetFileNameWithoutExtension(originalFileName);
        var containerPath = GetContainerPath(organizationId, officeName);
        var typeString = imageType.ToString().ToLowerInvariant();
        var includeTypeFolder = ShouldIncludeImageTypeFolder(officeName, imageType);
        var propertySegment = string.IsNullOrWhiteSpace(propertyCode)
            ? null
            : StoragePathBuilder.SanitizeSegment(propertyCode, "unknown-property", 80);
        var uploadsPath = includeTypeFolder
            ? Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar), typeString)
            : Path.Combine(_wwwRootPath, containerPath.Replace('/', Path.DirectorySeparatorChar));
        if (!string.IsNullOrWhiteSpace(propertySegment))
            uploadsPath = Path.Combine(uploadsPath, propertySegment);
        Directory.CreateDirectory(uploadsPath);

        var uniqueFileName = $"{fileNameOnly}-{Guid.NewGuid()}{effectiveExtension}";
        var fullPath = Path.Combine(uploadsPath, uniqueFileName);

        await using (var fileStreamOut = new FileStream(fullPath, FileMode.Create))
        {
            await streamToSave.CopyToAsync(fileStreamOut).ConfigureAwait(false);
        }

        var relativeDirectory = includeTypeFolder
            ? "/" + containerPath + "/" + typeString
            : "/" + containerPath;
        if (!string.IsNullOrWhiteSpace(propertySegment))
            relativeDirectory += "/" + propertySegment;
        return relativeDirectory + "/" + uniqueFileName;
    }

    public async Task<StoredImageRelocation> CopyToCorrectOfficeFolderAsync(string currentPath, int maxPathLength)
    {
        var relative = NormalizeRelative(currentPath);
        if (!TryCorrectOfficeSegment(relative.Trim('/'), out var correctedRelative))
        {
            return new StoredImageRelocation { AlreadyCorrect = true, OriginalPath = currentPath, NewPath = currentPath };
        }

        var newPath = "/" + correctedRelative;
        if (newPath.Length > maxPathLength)
            throw new InvalidOperationException("New path is longer than the database column.");

        var sourceFullPath = ToFullPath(relative);
        var targetFullPath = ToFullPath(newPath);
        if (!File.Exists(sourceFullPath))
        {
            if (File.Exists(targetFullPath))
                return new StoredImageRelocation { FoundExisting = true, OriginalPath = currentPath, NewPath = newPath };

            return new StoredImageRelocation { Missing = true, OriginalPath = currentPath };
        }

        var bytes = await File.ReadAllBytesAsync(sourceFullPath).ConfigureAwait(false);
        if (bytes.Length == 0)
            throw new InvalidOperationException("Stored file is empty.");

        Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath)!);
        await File.WriteAllBytesAsync(targetFullPath, bytes).ConfigureAwait(false);
        return new StoredImageRelocation
        {
            AlreadyCorrect = false,
            OriginalPath = currentPath,
            NewPath = newPath,
            Content = bytes,
            ContentType = "application/octet-stream"
        };
    }

    public Task<bool> DeleteStoredFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(false);

        var fullPath = ToFullPath(filePath);
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<int> ClearHyphenOfficePrefixAsync(Guid organizationId, string hyphenFolder)
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(hyphenFolder) || !hyphenFolder.Contains('-'))
            return Task.FromResult(0);

        var corrected = StoragePathBuilder.OfficeFolderName(hyphenFolder);
        if (string.Equals(corrected, hyphenFolder, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(0);

        var envName = (string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container).ToLowerInvariant();
        var sourceDir = Path.Combine(_wwwRootPath, envName, organizationId.ToString("N"), hyphenFolder.Trim().ToLowerInvariant());
        if (!Directory.Exists(sourceDir))
            return Task.FromResult(0);

        var destDir = Path.Combine(_wwwRootPath, envName, organizationId.ToString("N"), corrected);
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(destDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target))
                File.Delete(file);
            else
                File.Move(file, target);
            removed++;
        }

        if (Directory.Exists(sourceDir))
            Directory.Delete(sourceDir, recursive: true);

        return Task.FromResult(removed);
    }

    private static bool TryCorrectOfficeSegment(string blobName, out string correctedBlobName)
    {
        correctedBlobName = blobName;
        var parts = blobName.Split('/');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Length != 32 || !parts[i].All(Uri.IsHexDigit))
                continue;

            var office = Uri.UnescapeDataString(parts[i + 1]);
            var corrected = StoragePathBuilder.OfficeFolderName(office);
            if (string.Equals(office, corrected, StringComparison.Ordinal))
                return false;

            parts[i + 1] = corrected;
            correctedBlobName = string.Join("/", parts);
            return true;
        }

        return false;
    }

    public async Task<StoredImageRelocation> RelocateStoredImageAsync(string currentPath, Guid organizationId, string? officeName, string category, string? propertyCode, int maxPathLength)
    {
        var sourceFullPath = ToFullPath(currentPath);
        var envName = string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container.ToLowerInvariant();
        if (!File.Exists(sourceFullPath))
        {
            var missingFileName = Path.GetFileName(currentPath);
            if (!string.IsNullOrWhiteSpace(missingFileName))
            {
                var existingPath = "/" + envName + "/" + StoragePathBuilder.BuildDirectory(organizationId, officeName, category, propertyCode) + "/" + missingFileName;
                if (!string.Equals(NormalizeRelative(currentPath), existingPath, StringComparison.OrdinalIgnoreCase) && File.Exists(ToFullPath(existingPath)))
                {
                    if (existingPath.Length > maxPathLength)
                        throw new InvalidOperationException("New path is longer than the database column.");
                    return new StoredImageRelocation { FoundExisting = true, OriginalPath = currentPath, NewPath = existingPath };
                }
            }

            return new StoredImageRelocation { Missing = true, OriginalPath = currentPath };
        }

        var fileName = Path.GetFileName(sourceFullPath);
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("Stored file name is missing.");

        var newPath = "/" + envName + "/" + StoragePathBuilder.BuildDirectory(organizationId, officeName, category, propertyCode) + "/" + fileName;
        if (newPath.Length > maxPathLength)
            throw new InvalidOperationException("New path is longer than the database column.");

        var bytes = await File.ReadAllBytesAsync(sourceFullPath).ConfigureAwait(false);
        if (bytes.Length == 0)
            throw new InvalidOperationException("Stored file is empty.");

        if (string.Equals(NormalizeRelative(currentPath), newPath, StringComparison.OrdinalIgnoreCase))
        {
            return new StoredImageRelocation
            {
                AlreadyCorrect = true,
                OriginalPath = currentPath,
                NewPath = currentPath,
                Content = bytes,
                ContentType = "application/octet-stream"
            };
        }

        File.Delete(sourceFullPath);
        var targetFullPath = ToFullPath(newPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath)!);
            await File.WriteAllBytesAsync(targetFullPath, bytes).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFullPath)!);
            await File.WriteAllBytesAsync(sourceFullPath, bytes).ConfigureAwait(false);
            throw new InvalidOperationException("New file could not be saved. The original file was put back.", ex);
        }

        return new StoredImageRelocation
        {
            AlreadyCorrect = false,
            OriginalPath = currentPath,
            NewPath = newPath,
            Content = bytes,
            ContentType = "application/octet-stream"
        };
    }

    public Task<IReadOnlyList<string>> ListImageUrlsAsync(Guid organizationId, string? officeName, string category, string? propertyCode)
    {
        var envName = string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container.ToLowerInvariant();
        var relativeDirectory = "/" + envName + "/" + StoragePathBuilder.BuildDirectory(organizationId, officeName, category, propertyCode);
        var fullDirectory = ToFullPath(relativeDirectory);
        if (!Directory.Exists(fullDirectory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        IReadOnlyList<string> files = Directory.GetFiles(fullDirectory).Select(file => relativeDirectory + "/" + Path.GetFileName(file)).ToList();
        return Task.FromResult(files);
    }

    public Task<IReadOnlyList<string>> ListRootImagesAsync(Guid organizationId, string? officeName, string category, DateOnly? start, DateOnly? end)
    {
        var envName = string.IsNullOrWhiteSpace(_appSettings.Container) ? "dev" : _appSettings.Container.ToLowerInvariant();
        var relativeDirectory = "/" + envName + "/" + StoragePathBuilder.BuildDirectory(organizationId, officeName, category, null);
        var fullDirectory = ToFullPath(relativeDirectory);
        if (!Directory.Exists(fullDirectory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        IReadOnlyList<string> files = Directory.GetFiles(fullDirectory)
            .Where(file => IsWithinDates(File.GetLastWriteTimeUtc(file), start, end))
            .Select(file => relativeDirectory + "/" + Path.GetFileName(file))
            .ToList();
        return Task.FromResult(files);
    }

    private static bool IsWithinDates(DateTime modifiedUtc, DateOnly? start, DateOnly? end)
    {
        if (!start.HasValue && !end.HasValue)
            return true;

        var day = DateOnly.FromDateTime(modifiedUtc);
        if (start.HasValue && day < start.Value)
            return false;
        if (end.HasValue && day > end.Value)
            return false;
        return true;
    }

    public async Task RestoreStoredImageAsync(string originalPath, byte[] content, string contentType)
    {
        _ = contentType;
        var fullPath = ToFullPath(originalPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content).ConfigureAwait(false);
    }

    private string ToFullPath(string filePath)
    {
        var relative = filePath.Trim().TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_wwwRootPath, relative);
    }

    private static string NormalizeRelative(string filePath)
    {
        var relative = filePath.Trim().Replace('\\', '/');
        return relative.StartsWith('/') ? relative : "/" + relative;
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
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpeg", ".jpg", ".png", ".txt", ".json" };
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
            "text/plain",
            "application/json"
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

    public async Task<string> SavePropertyAgreementFileAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType)
    {
        var isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase);
        if (isPdf)
            return await SaveDocumentAsync(organizationId, officeName, fileStream, fileName, "application/pdf", DocumentType.Agreements).ConfigureAwait(false);

        var buffer = await ImageUploadLimits.ReadImageStreamWithSizeCapAsync(fileStream, _imageUploadSettings).ConfigureAwait(false);
        var (streamToSave, effectiveExtension, _) = await ImagePersistencePreparer.PrepareForSaveAsync(
            buffer, fileName, contentType, _imageUploadSettings, ImageType.OwnerAgreement).ConfigureAwait(false);
        try
        {
            var jpegName = Path.GetFileNameWithoutExtension(fileName) + effectiveExtension;
            var savedType = string.Equals(effectiveExtension, ".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "image/jpeg";
            return await SaveDocumentAsync(organizationId, officeName, streamToSave, jpegName, savedType, DocumentType.Agreements).ConfigureAwait(false);
        }
        finally
        {
            await streamToSave.DisposeAsync().ConfigureAwait(false);
        }
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
        var rawOffice = string.IsNullOrWhiteSpace(officeName) ? "global" : officeName.Trim();
        var slash = rawOffice.IndexOf('/');
        var officePart = slash >= 0 ? rawOffice[..slash] : rawOffice;
        var rest = slash >= 0 ? rawOffice[slash..].ToLowerInvariant() : string.Empty;
        var officeSegment = StoragePathBuilder.OfficeFolderName(officePart);
        return $"{envName}/{orgSegment}/{officeSegment}{rest}";
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
