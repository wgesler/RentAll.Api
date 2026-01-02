using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Infrastructure.Entities;

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
			DocumentType = (DocumentType)e.DocumentType,
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
}

