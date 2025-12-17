using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	public async Task<Building> CreateAsync(Building building)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("dbo.Building_Add", new
		{
			OrganizationId = building.OrganizationId,
			BuildingCode = building.BuildingCode,
			Description = building.Description,
			IsActive = building.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Building not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

