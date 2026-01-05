using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Regions;

public partial class RegionRepository : IRegionRepository
{
	public async Task<Region> UpdateByIdAsync(Region region)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<RegionEntity>("dbo.Region_UpdateById", new
		{
			RegionId = region.RegionId,
			OrganizationId = region.OrganizationId,
			OfficeId = region.OfficeId,
			RegionCode = region.RegionCode,
			Name = region.Name,
			Description = region.Description,
			IsActive = region.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Region not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}



