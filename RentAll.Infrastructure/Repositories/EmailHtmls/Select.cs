using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.EmailHtmls
{
	public partial class EmailHtmlRepository
	{
		public async Task<EmailHtml?> GetByOrganizationIdAsync(Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailHtmlEntity>("Email.EmailHtml_GetById", new
			{
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
