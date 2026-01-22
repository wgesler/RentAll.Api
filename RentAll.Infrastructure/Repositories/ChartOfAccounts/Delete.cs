using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.ChartOfAccounts;

public partial class ChartOfAccountRepository : IChartOfAccountRepository
{
	public async Task DeleteByIdAsync(int chartOfAccountId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.ChartOfAccount_DeleteById", new
		{
			ChartOfAccountId = chartOfAccountId,
			OrganizationId = organizationId
		});
	}
}
