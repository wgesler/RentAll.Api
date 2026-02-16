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
			var entity = ConvertModelToEntity(email);
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailStoredProcRow>("Email.Email_Add", new
			{
				OrganizationId = entity.OrganizationId,
				OfficeId = entity.OfficeId,
				PropertyId = entity.PropertyId,
				ReservationId = entity.ReservationId,
				ToRecipients = SerializeRecipients(entity.ToRecipients),
				CcRecipients = SerializeRecipients(entity.CcRecipients),
				BccRecipients = SerializeRecipients(entity.BccRecipients),
				FromRecipient = SerializeRecipient(entity.FromRecipient),
				Subject = entity.Subject,
				PlainTextContent = entity.PlainTextContent,
				HtmlContent = entity.HtmlContent,
				DocumentId = entity.DocumentId,
				AttachmentName = entity.AttachmentName,
				AttachmentPath = entity.AttachmentPath,
				EmailTypeId = entity.EmailTypeId,
				EmailStatusId = entity.EmailStatusId,
				AttemptCount = entity.AttemptCount,
				LastError = entity.LastError,
				LastAttemptedOn = entity.LastAttemptedOn,
				SentOn = entity.SentOn,
				CreatedBy = entity.CreatedBy
			});

			if (res == null || !res.Any())
				throw new Exception("Email not created");

			return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
		}
	}
}
