using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Franchises;

public partial class FranchiseRepository : IFranchiseRepository
{
	public async Task<IEnumerable<Franchise>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<FranchiseEntity>("dbo.Franchise_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<Franchise>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<Franchise?> GetByIdAsync(int franchiseId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<FranchiseEntity>("dbo.Franchise_GetById", new
		{
			FranchiseId = franchiseId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<Franchise?> GetByFranchiseCodeAsync(string franchiseCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<FranchiseEntity>("dbo.Franchise_GetByCode", new
		{
			FranchiseCode = franchiseCode,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByFranchiseCodeAsync(string franchiseCode, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("dbo.Franchise_ExistsByCode", new
		{
			FranchiseCode = franchiseCode,
			OrganizationId = organizationId
		});

		return result == 1;
	}
}

