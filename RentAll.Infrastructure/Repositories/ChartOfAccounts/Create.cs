using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	public async Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		var res = await db.DapperProcQueryAsync<ChartOfAccountEntity>("Accounting.ChartOfAccount_Add", new
		{
			OrganizationId = chartOfAccount.OrganizationId,
			AccountNumber = chartOfAccount.AccountNumber,
			Description = chartOfAccount.Description,
			AccountTypeId = (int)chartOfAccount.AccountType
		});

		if (res == null || !res.Any())
			throw new Exception("ChartOfAccount not created");

		return ConvertEntityToModel(res.FirstOrDefault()!);
	}
}
