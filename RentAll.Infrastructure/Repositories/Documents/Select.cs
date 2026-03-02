using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class DocumentRepository : IDocumentRepository
{
    public async Task<IEnumerable<Document>> GetDocumentsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Document>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByPropertyTypeAsync(Guid organizationId, Guid propertyId, int documentTypeId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetAllByPropertyType", new
        {
            OrganizationId = organizationId,
            PropertyId = propertyId,
            DocumentTypeId = documentTypeId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Document>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByOfficeIdAsync(int officeId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetAllByOfficeId", new
        {
            OfficeId = officeId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Document>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByDocumentTypeAsync(int documentType, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetAllByDocumentType", new
        {
            DocumentType = documentType,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Document>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Document?> GetDocumentByIdAsync(Guid documentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetById", new
        {
            DocumentId = documentId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Document?> GetDocumentByNameAsync(string fileName, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetByName", new
        {
            FileName = fileName,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

}
