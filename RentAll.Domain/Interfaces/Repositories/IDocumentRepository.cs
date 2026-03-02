using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
    #region Selects
    Task<IEnumerable<Document>> GetDocumentsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Document>> GetDocumentsByPropertyTypeAsync(Guid organizationId, Guid propertyId, int documentTypeId, string officeAccess);
    Task<IEnumerable<Document>> GetDocumentsByOfficeIdAsync(int officeId, Guid organizationId);
    Task<IEnumerable<Document>> GetDocumentsByDocumentTypeAsync(int documentType, Guid organizationId);
    Task<Document?> GetDocumentByIdAsync(Guid documentId, Guid organizationId);
    Task<Document?> GetDocumentByNameAsync(string fileName, Guid organizationId);
    #endregion

    #region Creates
    Task<Document> CreateAsync(Document document);
    #endregion

    #region Updates
    Task<Document> UpdateByIdAsync(Document document);
    #endregion

    #region Deletes
    Task DeleteDocumentByIdAsync(Guid documentId, Guid organizationId);
    #endregion
}
