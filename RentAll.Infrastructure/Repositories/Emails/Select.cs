using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Emails
{
	public partial class EmailRepository
	{
		public async Task<IEnumerable<Email>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailEntity>("Email.Email_GetAllByOfficeIds", new
			{
				OrganizationId = organizationId,
				Offices = officeAccess
			});

			if (res == null || !res.Any())
				return Enumerable.Empty<Email>();

			return res.Select(ConvertEntityToModel);
		}

		public async Task<Email?> GetByIdAsync(Guid emailId, Guid organizationId)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailEntity>("Email.Email_GetById", new
			{
				EmailId = emailId,
				OrganizationId = organizationId
			});

			if (res == null || !res.Any())
				return null;

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
