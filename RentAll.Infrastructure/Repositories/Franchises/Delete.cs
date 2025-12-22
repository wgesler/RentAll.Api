using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Franchises;

public partial class FranchiseRepository : IFranchiseRepository
{
	public async Task DeleteByIdAsync(int franchiseId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("dbo.Franchise_DeleteById", new
		{
			FranchiseId = franchiseId
		});
	}
}



