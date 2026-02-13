using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.EmailHtmls
{
	public partial class EmailHtmlRepository
	{
		public async Task<EmailHtml> UpdateByOrganizationIdAsync(EmailHtml emailHtml)
		{
			await using var db = new SqlConnection(_dbConnectionString);
			var res = await db.DapperProcQueryAsync<EmailHtmlEntity>("Email.EmailHtml_UpdateById", new
			{
				OrganizationId = emailHtml.OrganizationId,
				WelcomeLetter = emailHtml.WelcomeLetter,
				CorporateLetter = emailHtml.CorporateLetter,
				Lease = emailHtml.Lease,
				Invoice = emailHtml.Invoice,
				LetterSubject = emailHtml.LetterSubject,
				LeaseSubject = emailHtml.LeaseSubject,
				InvoiceSubject = emailHtml.InvoiceSubject,
				ModifiedBy = emailHtml.ModifiedBy
			});

			if (res == null || !res.Any())
				throw new Exception("EmailHtml not found");

			return ConvertEntityToModel(res.FirstOrDefault()!);
		}
	}
}
