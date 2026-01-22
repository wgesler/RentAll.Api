using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task<Company> UpdateByIdAsync(Company company)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_UpdateById", new
			{
				OrganizationId = company.OrganizationId,
				CompanyId = company.CompanyId,
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
				IsActive = company.IsActive,
				ModifiedBy = company.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Company not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
