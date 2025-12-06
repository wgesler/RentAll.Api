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

		private Company ConvertDtoToModel(CompanyEntity dto)
		{
			var response = new Company()
			{
				CompanyId = dto.CompanyId,
				CompanyCode = dto.CompanyCode,
				Name = dto.Name,
				Address1 = dto.Address1,
				Address2 = dto.Address2,
				City = dto.City,
				State = dto.State,
				Zip = dto.Zip,
				Phone = dto.Phone,
				Website = dto.Website,
				LogoStorageId = dto.LogoStorageId,
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