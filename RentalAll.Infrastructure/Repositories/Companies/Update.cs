using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task<Company> UpdateByIdAsync(Company company)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("dbo.Company_UpdateById", new
			{
				CompanyId = company.CompanyId,
				CompanyCode = company.CompanyCode,
				Name = company.Name,
				Address1 = company.Address1,
				Address2 = company.Address2,
				City = company.City,
				State = company.State,
				Zip = company.Zip,
				Phone = company.Phone,
				Website = company.Website,
				IsActive = company.IsActive,
				ModifiedBy = company.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Company not found");

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}
	}
}



