using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CompanyContacts
{
	public partial class CompanyContactRepository : ICompanyContactRepository
	{
		private readonly string _dbConnectionString;

		public CompanyContactRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private CompanyContact ConvertEntityToModel(CompanyContactEntity e)
		{
			var response = new CompanyContact()
			{
				ContactId = e.ContactId,
				CompanyId = e.CompanyId,
				IsActive = e.IsActive,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};

			return response;
		}
	}
}