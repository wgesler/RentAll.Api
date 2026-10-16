using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType, string? propertyCode = null);
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType, string? propertyCode = null);
    Task<StoredImageRelocation> RelocateStoredImageAsync(string currentPath, Guid organizationId, string? officeName, string category, string? propertyCode, int maxPathLength);
    Task<StoredImageRelocation> CopyToCorrectOfficeFolderAsync(string currentPath, int maxPathLength);
    Task<bool> DeleteStoredFileAsync(string filePath);
    Task<int> ClearHyphenOfficePrefixAsync(Guid organizationId, string hyphenFolder);
    Task<IReadOnlyList<string>> ListImageUrlsAsync(Guid organizationId, string? officeName, string category, string? propertyCode);
    Task<IReadOnlyList<string>> ListRootImagesAsync(Guid organizationId, string? officeName, string category, DateOnly? start, DateOnly? end);
    Task RestoreStoredImageAsync(string originalPath, byte[] content, string contentType);
    Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);
    Task<FileDetails?> GetImageDetailsAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);

    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType);
    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType);
    Task<string> SavePropertyAgreementFileAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath);
    Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath);
}
