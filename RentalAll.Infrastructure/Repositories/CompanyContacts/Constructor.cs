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

		private CompanyContact ConvertDtoToModel(CompanyContactEntity dto)
		{
			var response = new CompanyContact()
			{
				ContactId = dto.ContactId,
				CompanyId = dto.CompanyId,
				IsActive = dto.IsActive,
				CreatedOn = dto.CreatedOn,
				CreatedBy = dto.CreatedBy,
				ModifiedOn = dto.ModifiedOn,
				ModifiedBy = dto.ModifiedBy
			};

			return response;
		}
	}
}



