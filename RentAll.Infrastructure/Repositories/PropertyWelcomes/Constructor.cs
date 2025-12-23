using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.PropertyWelcomes
{
	public partial class PropertyWelcomeRepository : IPropertyWelcomeRepository
	{
		private readonly string _dbConnectionString;

		public PropertyWelcomeRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private PropertyWelcome ConvertEntityToModel(PropertyWelcomeEntity e)
		{
			var response = new PropertyWelcome()
			{
				PropertyId = e.PropertyId,
				OrganizationId = e.OrganizationId,
				WelcomeLetter = e.WelcomeLetter,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}


