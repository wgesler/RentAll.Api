using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Interfaces.Services;

public interface IFileService
{
	Task<string> SaveLogoAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, EntityType entityType);
	Task<string> SaveLogoAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, EntityType entityType);
	Task<bool> DeleteLogoAsync(Guid organizationId, int? officeId, string filePath);
	Task<FileDetails?> GetFileDetailsAsync(Guid organizationId, int? officeId, string filePath);
	
	Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, string fileContent, string fileName, string contentType, DocumentType documentType);
	Task<string> SaveDocumentAsync(Guid organizationId, int? officeId, Stream fileStream, string fileName, string contentType, DocumentType documentType);
	Task<bool> DeleteDocumentAsync(Guid organizationId, int? officeId, string filePath);
	Task<FileDetails?> GetDocumentDetailsAsync(Guid organizationId, int? officeId, string filePath);
}

