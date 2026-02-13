using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Emails
{
	public partial class EmailRepository
	{
		public async Task<Email> CreateAsync(Email email)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailEntity>("Email.Email_Add", new
			{
				OrganizationId = email.OrganizationId,
				OfficeId = email.OfficeId,
				FromEmail = email.FromEmail,
				FromName = email.FromName,
				ToEmail = email.ToEmail,
				ToName = email.ToName,
				Subject = email.Subject,
				PlainTextContent = email.PlainTextContent,
				HtmlContent = email.HtmlContent,
				AttachmentPath = email.AttachmentPath,
				EmailStatusId = (int)email.EmailStatus,
				AttemptCount = email.AttemptCount,
				LastError = email.LastError,
				LastAttemptedOn = email.LastAttemptedOn,
				SentOn = email.SentOn,
				CreatedBy = email.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Email not created");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
