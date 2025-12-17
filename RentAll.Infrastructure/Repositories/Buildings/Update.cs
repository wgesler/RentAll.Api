using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	public async Task<Building> UpdateByIdAsync(Building building)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("dbo.Building_UpdateById", new
		{
			BuildingId = building.BuildingId,
			OrganizationId = building.OrganizationId,
			BuildingCode = building.BuildingCode,
			Description = building.Description,
			IsActive = building.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Building not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

