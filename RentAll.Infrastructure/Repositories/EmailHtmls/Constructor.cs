using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.EmailHtmls
{
	public partial class EmailHtmlRepository : IEmailHtmlRepository
	{
		private readonly string _dbConnectionString;

		public EmailHtmlRepository(IOptions<AppSettings> appSettings)
		{
			_dbConnectionString = appSettings.Value.DbConnections
				.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!
				.ConnectionString;
		}

		private EmailHtml ConvertEntityToModel(EmailHtmlEntity e)
		{
			return new EmailHtml
			{
				OrganizationId = e.OrganizationId,
				WelcomeLetter = e.WelcomeLetter,
				CorporateLetter = e.CorporateLetter,
				Lease = e.Lease,
				Invoice = e.Invoice,
				LetterSubject = e.LetterSubject,
				LeaseSubject = e.LeaseSubject,
				InvoiceSubject = e.InvoiceSubject,
				CreatedOn = e.CreatedOn,
				CreatedBy = e.CreatedBy,
				ModifiedOn = e.ModifiedOn,
				ModifiedBy = e.ModifiedBy
			};
		}
	}
}
