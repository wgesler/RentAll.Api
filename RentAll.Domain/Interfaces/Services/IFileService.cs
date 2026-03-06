using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, ImageType imageType);
    Task<string> SaveImageAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, ImageType imageType);
    Task<bool> DeleteImageAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);
    Task<FileDetails?> GetImageDetailsAsync(Guid organizationId, string? officeName, string filePath, ImageType imageType);

    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, string fileContent, string fileName, string contentType, DocumentType documentType);
    Task<string> SaveDocumentAsync(Guid organizationId, string? officeName, Stream fileStream, string fileName, string contentType, DocumentType documentType);
    Task<bool> DeleteDocumentAsync(Guid organizationId, string? officeName, string filePath);
    Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, string? officeName, string filePath);
}

