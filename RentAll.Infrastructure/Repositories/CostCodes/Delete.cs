using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.CostCodes;

public partial class CostCodeRepository : ICostCodeRepository
{
	public async Task DeleteByIdAsync(int costCodeId, int officeId, Guid organizationId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Accounting.CostCode_DeleteById", new
		{
			CostCodeId = costCodeId,
			OrganizationId = organizationId,
			OfficeId = officeId
		});
	}
}
