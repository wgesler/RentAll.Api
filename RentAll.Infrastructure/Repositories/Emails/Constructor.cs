using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Emails
{
	public partial class EmailRepository : IEmailRepository
	{
		private readonly string _dbConnectionString;

		public EmailRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections
				.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!
				.ConnectionString;
		}

		private Email ConvertEntityToModel(EmailEntity e)
		{
			return new Email
			{
				EmailId = e.EmailId,
				OrganizationId = e.OrganizationId,
				OfficeId = e.OfficeId,
				ToEmail = e.ToEmail,
				ToName = e.ToName,
				FromEmail = e.FromEmail,
				FromName = e.FromName,
				Subject = e.Subject,
				PlainTextContent = e.PlainTextContent,
				HtmlContent = e.HtmlContent,
				EmailStatus = (EmailStatus)e.EmailStatusId,
				AttemptCount = e.AttemptCount,
				LastError = e.LastError,
				LastAttemptedOn = e.LastAttemptedOn,
				SentOn = e.SentOn,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};
		}
	}
}
