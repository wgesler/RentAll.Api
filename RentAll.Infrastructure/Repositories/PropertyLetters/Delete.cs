using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.PropertyLetters
{
	public partial class PropertyLetterRepository : IPropertyLetterRepository
	{
	public async Task DeleteByPropertyIdAsync(Guid propertyId)
	{
		await using var db = new SqlConnection(_dbConnectionString);
		await db.DapperProcExecuteAsync("Property.PropertyInformation_DeleteByPropertyId", new
		{
			PropertyId = propertyId
		});
	}
	}
}

