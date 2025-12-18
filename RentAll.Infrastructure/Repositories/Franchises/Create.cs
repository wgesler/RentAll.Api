using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Franchises;

public partial class FranchiseRepository : IFranchiseRepository
{
	public async Task<Franchise> CreateAsync(Franchise franchise)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<FranchiseEntity>("dbo.Franchise_Add", new
		{
			OrganizationId = franchise.OrganizationId,
			FranchiseCode = franchise.FranchiseCode,
			Description = franchise.Description,
			IsActive = franchise.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("Franchise not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}

