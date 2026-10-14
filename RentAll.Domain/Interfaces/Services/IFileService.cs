using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType, string? propertyCode = null);
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType, string? propertyCode = null);
    Task<StoredImageRelocation> RelocateStoredImageAsync(string currentPath, Guid organizationId, string? officeName, string category, string? propertyCode, int maxPathLength);
    Task<StoredImageRelocation> MoveStoredFileOfficeAsync(string currentPath, string oldOfficeFolder, string newOfficeFolder, int maxPathLength);
    Task<IReadOnlyList<string>> ListImageUrlsAsync(Guid organizationId, string? officeName, string category, string? propertyCode);
    Task RestoreStoredImageAsync(string originalPath, byte[] content, string contentType);
    Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);
    Task<FileDetails?> GetImageDetailsAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);

    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType);
    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType);
    Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath);
    Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath);
}
