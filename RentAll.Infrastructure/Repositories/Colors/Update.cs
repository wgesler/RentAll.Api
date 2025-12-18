using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Colors;

public partial class ColorRepository : IColorRepository
{
	public async Task UpdateByIdAsync(Colour c)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ColorEntity>("dbo.Color_UpdateById", new
		{
			ColorId = c.ColorId,
			OrganizationId = c.OrganizationId,
			ReservationStatusId = c.ReservationStatusId,
			Color = c.Color
		});
	}
}

