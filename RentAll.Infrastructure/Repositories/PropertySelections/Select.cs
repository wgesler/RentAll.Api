using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertySelections;

public partial class PropertySelectionRepository : IPropertySelectionRepository
{
	public async Task<PropertySelection?> GetByUserIdAsync(Guid userId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<PropertySelectionEntity>("Property.PropertySelection_GetByUserId", new
		{
			UserId = userId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.First()!);
	}
}




