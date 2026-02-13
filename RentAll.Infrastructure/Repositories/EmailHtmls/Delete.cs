using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.EmailHtmls
{
	public partial class EmailHtmlRepository
	{
		public async Task DeleteByOrganizationIdAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("Email.EmailHtml_DeleteById", new
			{
				OrganizationId = organizationId
			});
		}
	}
}
