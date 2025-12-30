using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Franchises;

public partial class FranchiseRepository : IFranchiseRepository
{
	public async Task<Franchise> UpdateByIdAsync(Franchise franchise)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<FranchiseEntity>("dbo.Franchise_UpdateById", new
		{
			FranchiseId = franchise.FranchiseId,
			OrganizationId = franchise.OrganizationId,
			FranchiseCode = franchise.FranchiseCode,
			Description = franchise.Description,
			Phone = franchise.Phone,
			IsActive = franchise.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Franchise not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}



