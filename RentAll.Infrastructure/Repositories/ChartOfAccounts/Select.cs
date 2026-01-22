using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	public async Task<IEnumerable<ChartOfAccount>> GetAllAsync(Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetAll", new
		{
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return Enumerable.Empty<ChartOfAccount>();

		return res.Select(ConvertEntityToModel);
	}

	public async Task<ChartOfAccount?> GetByIdAsync(int chartOfAccountId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetById", new
		{
			ChartOfAccountId = chartOfAccountId,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<ChartOfAccount?> GetByAccountNumberAsync(string accountNumber, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetByAccountNumber", new
		{
			AccountNumber = accountNumber,
			OrganizationId = organizationId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByAccountNumberAsync(string accountNumber, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("Accounting.ChartOfAccount_ExistsByAccountNumber", new
		{
			AccountNumber = accountNumber,
			OrganizationId = organizationId
		});

		return result == 1;
	}
}
