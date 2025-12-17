using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	public async Task<Area> UpdateByIdAsync(Area area)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("dbo.Area_UpdateById", new
		{
			AreaId = area.AreaId,
			OrganizationId = area.OrganizationId,
			AreaCode = area.AreaCode,
			Description = area.Description,
			IsActive = area.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Area not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

