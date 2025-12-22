using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository : IOrganizationRepository
{
	public async Task DeleteByIdAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("dbo.Organization_DeleteById", new
		{
			OrganizationId = organizationId
		});
	}
}




