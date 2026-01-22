using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	public async Task<Building> CreateAsync(Building building)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_Add", new
		{
			OrganizationId = building.OrganizationId,
			OfficeId = building.OfficeId,
			BuildingCode = building.BuildingCode,
			Name = building.Name,
			Description = building.Description,
			HoaName = building.HoaName,
			HoaPhone = building.HoaPhone,
			HoaEmail = building.HoaEmail,
			IsActive = building.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Building not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}



