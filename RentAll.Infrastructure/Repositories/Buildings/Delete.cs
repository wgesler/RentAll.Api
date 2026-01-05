using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	public async Task DeleteByIdAsync(int buildingId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("dbo.Building_DeleteById", new
		{
			BuildingId = buildingId
		});
	}
}



