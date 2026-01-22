using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Documents;

public partial class DocumentRepository : IDocumentRepository
{
	public async Task DeleteByIdAsync(Guid documentId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Organization.Document_DeleteById", new
		{
			DocumentId = documentId,
			OrganizationId = organizationId
		});
	}
}

