using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.CodeSequences;

public partial class CodeSequenceRepository : ICodeSequenceRepository
{
	public async Task<int> GetNextAsync(Guid organizationId, int entityTypeId, string entityType)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("Organization.CodeSequence_GetNext", new
		{
			OrganizationId = organizationId,
			EntityTypeId = entityTypeId,
			EntityType = entityType
		});

		return result;
	}
}

