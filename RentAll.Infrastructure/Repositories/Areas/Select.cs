using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	public async Task<IEnumerable<Area>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("dbo.Area_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Area>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Area?> GetByIdAsync(int areaId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("dbo.Area_GetById", new
		{
			AreaId = areaId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Area?> GetByAreaCodeAsync(string areaCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("dbo.Area_GetByCode", new
		{
			AreaCode = areaCode,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByAreaCodeAsync(string areaCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("dbo.Area_ExistsByCode", new
		{
			AreaCode = areaCode,
			OrganizationId = organizationId
		});

		return result == 1;
	}
}

