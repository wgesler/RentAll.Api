using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Companies;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.CompanyContacts
{
	public partial class CompanyContactRepository : ICompanyContactRepository
	{
		public async Task<CompanyContact> UpdateByIdAsync(CompanyContact companyContact)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<CompanyContactEntity>("dbo.CompanyContact_UpdateById", new
			{
				ContactId = companyContact.ContactId,
				CompanyId = companyContact.CompanyId,
				IsActive = companyContact.IsActive,
				ModifiedBy = companyContact.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("CompanyContact not found");

			return ConvertDtoToModel(res.FirstOrDefault()!);
		}
	}
}



