using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
	// Creates
	Task<Document> CreateAsync(Document document);

	// Selects
	Task<IEnumerable<Document>> GetAllAsync(Guid organizationId);
	Task<IEnumerable<Document>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
	Task<IEnumerable<Document>> GetAllByPropertyTypeAsync(Guid organizationId, Guid propertyId, int documentTypeId, string officeAccess);
	Task<Document?> GetByIdAsync(Guid documentId, Guid organizationId);
	Task<Document?> GetByNameAsync(string fileName, Guid organizationId);
	Task<IEnumerable<Document>> GetByOfficeIdAsync(int officeId, Guid organizationId);
	Task<IEnumerable<Document>> GetByDocumentTypeAsync(int documentType, Guid organizationId);

	// Updates
	Task<Document> UpdateByIdAsync(Document document);

	// Deletes (soft delete)
	Task DeleteByIdAsync(Guid documentId, Guid organizationId);
}


