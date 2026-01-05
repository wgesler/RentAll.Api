using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Offices;

public partial class OfficeRepository : IOfficeRepository
{
	public async Task DeleteByIdAsync(int officeId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("dbo.Office_DeleteById", new
		{
			OfficeId = officeId
		});
	}
}

