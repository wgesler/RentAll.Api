using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.LeaseInformations
{
	public partial class LeaseInformationRepository : ILeaseInformationRepository
	{
		public async Task DeleteByIdAsync(Guid leaseInformationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.LeaseInformation_DeleteById", new
			{
				LeaseInformationId = leaseInformationId
			});
		}
	}
}

