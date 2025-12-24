using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyWelcomes
{
	public partial class PropertyWelcomeRepository : IPropertyWelcomeRepository
	{
		public async Task<PropertyWelcome> CreateAsync(PropertyWelcome propertyWelcome)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<PropertyWelcomeEntity>("dbo.PropertyWelcome_Add", new
			{
				PropertyId = propertyWelcome.PropertyId,
				OrganizationId = propertyWelcome.OrganizationId,
				WelcomeLetter = propertyWelcome.WelcomeLetter,
				CreatedBy = propertyWelcome.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("PropertyWelcome not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}


