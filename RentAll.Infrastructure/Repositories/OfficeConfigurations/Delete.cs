using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.OfficeConfigurations;

public partial class OfficeConfigurationRepository : IOfficeConfigurationRepository
{
	public async Task DeleteByOfficeIdAsync(int officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("dbo.OfficeConfiguration_DeleteByOfficeId", new
		{
			OfficeId = officeId
		});
	}
}

