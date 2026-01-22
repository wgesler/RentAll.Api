using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Buildings;

public partial class BuildingRepository : IBuildingRepository
{
	public async Task<IEnumerable<Building>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Building>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Building>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetAllByOfficeId", new
		{
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Building>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Building?> GetByIdAsync(int buildingId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetById", new
		{
			BuildingId = buildingId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Building?> GetByBuildingCodeAsync(string buildingCode, Guid organizationId, int? officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetByCode", new
		{
			BuildingCode = buildingCode,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByBuildingCodeAsync(string buildingCode, Guid organizationId, int? officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("Organization.Building_ExistsByCode", new
		{
			BuildingCode = buildingCode,
			OrganizationId = organizationId,
			OfficeId = officeId

		});

		return result == 1;
	}
}



