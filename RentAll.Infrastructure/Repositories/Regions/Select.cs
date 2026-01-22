using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Regions;

public partial class RegionRepository : IRegionRepository
{
	public async Task<IEnumerable<Region>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Region>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<IEnumerable<Region>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetAllByOfficeId", new
		{
			OrganizationId = organizationId,
			Offices = officeAccess
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Region>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Region?> GetByIdAsync(int regionId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetById", new
		{
			RegionId = regionId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Region?> GetByRegionCodeAsync(string regionCode, Guid organizationId, int? officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetByCode", new
		{
			RegionCode = regionCode,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByRegionCodeAsync(string regionCode, Guid organizationId, int? officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("Organization.Region_ExistsByCode", new
		{
			RegionCode = regionCode,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		return result == 1;
	}
}



