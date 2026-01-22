using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task<Company> CreateAsync(Company company)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_Add", new
			{
				OrganizationId = company.OrganizationId,
				OfficeId = company.OfficeId,
				CompanyCode = company.CompanyCode,
				Name = company.Name,
				Address1 = company.Address1,
				Address2 = company.Address2,
				Suite = company.Suite,
				City = company.City,
				State = company.State,
				Zip = company.Zip,
				Phone = company.Phone,
				Website = company.Website,
				LogoPath = company.LogoPath,
				Notes = company.Notes,
				CreatedBy = company.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Company not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
