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
		var res = await db.DapperProcQueryAsync<RegionEntity>("dbo.Region_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Region>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Region?> GetByIdAsync(int regionId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("dbo.Region_GetById", new
		{
			RegionId = regionId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Region?> GetByRegionCodeAsync(string regionCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("dbo.Region_GetByCode", new
		{
			RegionCode = regionCode,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByRegionCodeAsync(string regionCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("dbo.Region_ExistsByCode", new
		{
			RegionCode = regionCode,
			OrganizationId = organizationId
		});

		return result == 1;
	}
}

