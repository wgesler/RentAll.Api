using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Emails
{
	public partial class EmailRepository
	{
		public async Task<Email> UpdateByIdAsync(Email email)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailEntity>("Email.Email_UpdateById", new
			{
				EmailId = email.EmailId,
				OrganizationId = email.OrganizationId,
				EmailStatusId = (int)email.EmailStatus,
				AttemptCount = email.AttemptCount,
				LastError = email.LastError,
				LastAttemptedOn = email.LastAttemptedOn,
				SentOn = email.SentOn,
				ModifiedBy = email.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Email not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
