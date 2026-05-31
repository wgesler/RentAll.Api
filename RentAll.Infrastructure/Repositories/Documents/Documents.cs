using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class DocumentRepository : IDocumentRepository
{
    private readonly string _dbConnectionString;

    public DocumentRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private Document ConvertEntityToModel(DocumentEntity e)
    {
        return new Document
        {
            DocumentId = e.DocumentId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            DocumentType = (DocumentType)e.DocumentTypeId,
            FileName = e.FileName,
            FileExtension = e.FileExtension,
            ContentType = e.ContentType,
            DocumentPath = e.DocumentPath,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    #region Selects
    public async Task<IEnumerable<Document>> GetDocumentsAsync(DocumentGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            DocumentTypeIds = criteria.DocumentTypeIds,
            StartDate = criteria.StartDate.HasValue ? criteria.StartDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            EndDate = criteria.EndDate.HasValue ? criteria.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
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
    #endregion

    #region Creates
    public async Task<Document> CreateAsync(Document document)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_Add", new
        {
            OrganizationId = document.OrganizationId,
            OfficeId = document.OfficeId,
            PropertyId = document.PropertyId,
            ReservationId = document.ReservationId,
            DocumentTypeId = (int)document.DocumentType,
            FileName = document.FileName,
            FileExtension = document.FileExtension,
            ContentType = document.ContentType,
            DocumentPath = document.DocumentPath,
            CreatedBy = document.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Document not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<Document> UpdateByIdAsync(Document document)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DocumentEntity>("Organization.Document_UpdateById", new
        {
            DocumentId = document.DocumentId,
            OrganizationId = document.OrganizationId,
            OfficeId = document.OfficeId,
            PropertyId = document.PropertyId,
            ReservationId = document.ReservationId,
            DocumentTypeId = (int)document.DocumentType,
            FileName = document.FileName,
            FileExtension = document.FileExtension,
            ContentType = document.ContentType,
            DocumentPath = document.DocumentPath,
            ModifiedBy = document.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Document not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteDocumentByIdAsync(Guid documentId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Document_DeleteById", new
        {
            DocumentId = documentId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
