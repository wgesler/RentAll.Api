using Microsoft.Data.SqlClient;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Colors;

public partial class ColorRepository : IColorRepository
{
	public async Task<IEnumerable<Colour>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ColorEntity>("dbo.Color_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Colour>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Colour?> GetByIdAsync(int colorId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ColorEntity>("dbo.Color_GetById", new
		{
			ColorId = colorId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

