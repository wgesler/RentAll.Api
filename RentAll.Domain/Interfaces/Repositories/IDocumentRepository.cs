using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
    #region Selects
    Task<IEnumerable<Document>> GetDocumentsAsync(DocumentGetCriteria criteria);
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
