using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.PropertyHtmls
{
	public partial class PropertyHtmlRepository : IPropertyHtmlRepository
	{
		public async Task DeleteByPropertyIdAsync(Guid propertyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("Property.PropertyHtml_DeleteByPropertyId", new
			{
				PropertyId = propertyId
			});
		}
	}
}


