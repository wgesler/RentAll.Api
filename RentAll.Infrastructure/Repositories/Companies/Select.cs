using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task<IEnumerable<Company>> GetAllAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetAll", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Company>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<IEnumerable<Company>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetAllByOfficeId", new
			{
				OrganizationId = organizationId,
				Offices = officeAccess
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Company>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<Company?> GetByIdAsync(Guid companyId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetById", new
			{
				CompanyId = companyId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}

		public async Task<Company?> GetByCompanyCodeAsync(string companyCode, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("Organization.Company_GetByCode", new
			{
				CompanyCode = companyCode,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
		public async Task<bool> ExistsByCompanyCodeAsync(string companyCode, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var result = await db.DapperProcQueryScalarAsync<int>("Organization.Company_ExistsByCode", new
			{
				CompanyCode = companyCode,
				OrganizationId = organizationId
			});

			return result == 1;
		}

	}
}
