using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CompanyContacts
{
	public partial class CompanyContactRepository : ICompanyContactRepository
	{
		public async Task<CompanyContact> CreateAsync(CompanyContact companyContact)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyContactEntity>("dbo.CompanyContact_Add", new
			{
				ContactId = companyContact.ContactId,
				CompanyId = companyContact.CompanyId,
				CreatedBy = companyContact.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("CompanyContact not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}