using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Companies
{
	public partial class CompanyRepository : ICompanyRepository
	{
		public async Task DeleteByIdAsync(Guid companyId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.Company_DeleteById", new
			{
				CompanyId = companyId
			});
		}
	}
}