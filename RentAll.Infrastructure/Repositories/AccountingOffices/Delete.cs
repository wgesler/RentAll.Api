using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.AccountingOffices;

public partial class AccountingOfficeRepository : IAccountingOfficeRepository
{
	public async Task DeleteAsync(Guid organizationId, int officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.Office_Delete", new
		{
			OrganizationId = organizationId,
			OfficeId = officeId
		});
	}
}
