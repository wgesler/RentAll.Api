using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Areas;

public partial class AreaRepository : IAreaRepository
{
	public async Task<Area> UpdateByIdAsync(Area area)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_UpdateById", new
		{
			AreaId = area.AreaId,
			OrganizationId = area.OrganizationId,
			OfficeId = area.OfficeId,
			AreaCode = area.AreaCode,
			Name = area.Name,
			Description = area.Description,
			IsActive = area.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Area not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}



