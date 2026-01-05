using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class DocumentRepository : IDocumentRepository
{
	public async Task<Document> UpdateByIdAsync(Document document)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<DocumentEntity>("dbo.Document_UpdateById", new
		{
			DocumentId = document.DocumentId,
			OrganizationId = document.OrganizationId,
			OfficeId = document.OfficeId,
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
}

