using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
    Task<string> SavePhotoAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType);
    Task<string> SaveReceiptAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType);
    Task<string> SaveLogoAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, EntityType entityType);
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, EntityType entityType, ImageType imageType);
    Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);
    Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, string? officeName, string filePath);

    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType);
    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType);
    Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath);
    Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath);
}

