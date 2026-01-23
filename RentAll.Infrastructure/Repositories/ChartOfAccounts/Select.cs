using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	public async Task<List<ChartOfAccount>> GetAllByOfficeIdAsync(int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetAllByOfficeId", new
		{
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return new List<ChartOfAccount>();

		return res.Select(ConvertEntityToModel).ToList();
	}

	public async Task<ChartOfAccount?> GetByIdAsync(int chartOfAccountId, int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetById", new
		{
			ChartOfAccountId = chartOfAccountId,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<ChartOfAccount?> GetByAccountNumberAsync(int accountNumber, int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_GetByAccountNumber", new
		{
			AccountNumber = accountNumber,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		if (res == null || !res.Any())
			return null;

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}

	public async Task<bool> ExistsByAccountNumberAsync(string accountNumber, int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var result = await db.DapperProcQueryScalarAsync<int>("Accounting.ChartOfAccount_ExistsByAccountNumber", new
		{
			AccountNumber = accountNumber,
			OrganizationId = organizationId,
			OfficeId = officeId
		});

		return result == 1;
	}
}
