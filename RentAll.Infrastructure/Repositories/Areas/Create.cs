using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	public async Task<Area> CreateAsync(Area area)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("dbo.Area_Add", new
		{
			OrganizationId = area.OrganizationId,
			AreaCode = area.AreaCode,
			Description = area.Description,
			IsActive = area.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Area not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

