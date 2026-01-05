using Microsoft.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyWelcomes
{
	public partial class PropertyWelcomeRepository : IPropertyWelcomeRepository
	{
		public async Task<PropertyWelcome> UpdateByIdAsync(PropertyWelcome propertyWelcome)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyWelcomeEntity>("dbo.PropertyWelcome_UpsertByPropertyId", new
			{
				PropertyId = propertyWelcome.PropertyId,
				OrganizationId = propertyWelcome.OrganizationId,
				WelcomeLetter = propertyWelcome.WelcomeLetter,
				ModifiedBy = propertyWelcome.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("PropertyWelcome not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


