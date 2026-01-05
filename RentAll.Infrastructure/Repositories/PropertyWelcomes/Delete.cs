using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.PropertyWelcomes
{
	public partial class PropertyWelcomeRepository : IPropertyWelcomeRepository
	{
		public async Task DeleteByPropertyIdAsync(Guid propertyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.PropertyWelcome_DeleteByPropertyId", new
			{
				PropertyId = propertyId
			});
		}
	}
}


