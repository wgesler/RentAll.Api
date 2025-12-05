using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task<Company?> GetByIdAsync(Guid companyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("dbo.Company_GetById", new
			{
				CompanyId = companyId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}

		public async Task<IEnumerable<Company>> GetAllAsync()
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("dbo.Company_GetAll", null);

			if (res == null || !res.Any())
				return Enumerable.Empty<Company>();

			return res.Select(ConvertDtoToModel);
		}

		public async Task<bool> ExistsByCompanyCodeAsync(string companyCode)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyEntity>("dbo.Company_GetByCompanyCode", new
			{
				CompanyCode = companyCode
			});

			if (res == null || !res.Any())
				return false;
			return true;
		}
	}
}