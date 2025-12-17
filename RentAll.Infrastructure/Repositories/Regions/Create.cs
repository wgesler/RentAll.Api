using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Regions;

public partial class RegionRepository : IRegionRepository
{
	public async Task<Region> CreateAsync(Region region)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("dbo.Region_Add", new
		{
			OrganizationId = region.OrganizationId,
			RegionCode = region.RegionCode,
			Description = region.Description,
			IsActive = region.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Region not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

