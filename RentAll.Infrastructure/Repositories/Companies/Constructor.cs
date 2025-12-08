using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		private readonly string _dbConnectionString;

		public CompanyRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
		}

		private Company ConvertEntityToModel(CompanyEntity e)
		{
			var response = new Company()
			{
				CompanyId = e.CompanyId,
				CompanyCode = e.CompanyCode,
				ContactId = e.ContactId,
				Name = e.Name,
				Address1 = e.Address1,
				Address2 = e.Address2,
				City = e.City,
				State = e.State,
				Zip = e.Zip,
				Phone = e.Phone,
				Website = e.Website,
				LogoStorageId = e.LogoStorageId,
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