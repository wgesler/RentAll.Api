using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CompanyContacts
{
	public partial class CompanyContactRepository : ICompanyContactRepository
	{
		public async Task<CompanyContact?> GetByIdAsync(Guid contactId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyContactEntity>("dbo.CompanyContact_GetById", new
			{
				ContactId = contactId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}

		public async Task<IEnumerable<CompanyContact>> GetByCompanyIdAsync(Guid companyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyContactEntity>("dbo.CompanyContact_GetByCompanyId", new
			{
				CompanyId = companyId
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<CompanyContact>();

			return res.Select(ConvertDtoToModel);
		}
	}
}



