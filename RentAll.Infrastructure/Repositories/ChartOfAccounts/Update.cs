using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	public async Task<ChartOfAccount> UpdateByIdAsync(ChartOfAccount chartOfAccount)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_UpdateById", new
		{
			ChartOfAccountId = chartOfAccount.ChartOfAccountId,
			OrganizationId = chartOfAccount.OrganizationId,
			OfficeId = chartOfAccount.OfficeId,
			AccountId = chartOfAccount.AccountId,
			Description = chartOfAccount.Description,
			AccountTypeId = (int)chartOfAccount.AccountType,
			IsActive = chartOfAccount.IsActive
		});

		if (res == null || !res.Any())
			throw new Exception("ChartOfAccount not found");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}
