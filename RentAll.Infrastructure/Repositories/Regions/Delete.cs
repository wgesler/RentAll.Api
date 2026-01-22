using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Regions;

public partial class RegionRepository : IRegionRepository
{
	public async Task DeleteByIdAsync(int regionId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Organization.Region_DeleteById", new
		{
			RegionId = regionId
		});
	}
}



