using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class DocumentRepository : IDocumentRepository
{
	public async Task<Document> CreateAsync(Document document)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<DocumentEntity>("dbo.Document_Add", new
		{
			OrganizationId = document.OrganizationId,
			OfficeId = document.OfficeId,
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
}

