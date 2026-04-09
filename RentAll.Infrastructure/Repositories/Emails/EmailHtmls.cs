using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Emails
{
    public partial class EmailRepository
    {
        #region Selects
        public async Task<EmailHtml?> GetEmailHtmlByOrganizationIdAsync(Guid organizationId)
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
        #endregion

        #region Creates
        public async Task<EmailHtml> CreateEmailHtmlAsync(EmailHtml emailHtml)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailHtmlEntity>("Email.EmailHtml_Add", new
            {
                OrganizationId = emailHtml.OrganizationId,
                WelcomeLetter = emailHtml.WelcomeLetter,
                CorporateLetter = emailHtml.CorporateLetter,
                Lease = emailHtml.Lease,
                CorporateLease = emailHtml.CorporateLease,
                Invoice = emailHtml.Invoice,
                CorporateInvoice = emailHtml.CorporateInvoice,
                LetterSubject = emailHtml.LetterSubject,
                LeaseSubject = emailHtml.LeaseSubject,
                InvoiceSubject = emailHtml.InvoiceSubject,
                CreatedBy = emailHtml.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("EmailHtml not created");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<EmailHtml> UpdateEmailHtmlByOrganizationIdAsync(EmailHtml emailHtml)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailHtmlEntity>("Email.EmailHtml_UpdateById", new
            {
                OrganizationId = emailHtml.OrganizationId,
                WelcomeLetter = emailHtml.WelcomeLetter,
                CorporateLetter = emailHtml.CorporateLetter,
                Lease = emailHtml.Lease,
                CorporateLease = emailHtml.CorporateLease,
                Invoice = emailHtml.Invoice,
                CorporateInvoice = emailHtml.CorporateInvoice,
                LetterSubject = emailHtml.LetterSubject,
                LeaseSubject = emailHtml.LeaseSubject,
                InvoiceSubject = emailHtml.InvoiceSubject,
                ModifiedBy = emailHtml.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("EmailHtml not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeleteEmailHtmlByOrganizationIdAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Email.EmailHtml_DeleteById", new
            {
                OrganizationId = organizationId
            });
        }
        #endregion
    }
}
