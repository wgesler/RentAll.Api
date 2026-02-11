using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	public async Task DeleteByIdAsync(int areaId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Organization.Area_DeleteById", new
		{
			AreaId = areaId
		});
	}
}



