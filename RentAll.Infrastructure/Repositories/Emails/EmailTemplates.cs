using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Emails
{
    public partial class EmailRepository
    {
        #region EmailTemplate Selects
        /// <summary>The office's template for this email, or the organization-wide one when the office has none.</summary>
        public async Task<EmailTemplate?> GetEmailTemplateByNameAsync(Guid organizationId, int? officeId, string emailName)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailTemplateEntity>("Email.EmailTemplate_GetByName", new
            {
                OrganizationId = organizationId,
                OfficeId = officeId,
                EmailName = emailName
            });

            var e = res?.FirstOrDefault();
            return e == null ? null : ConvertEntityToModel(e);
        }

        public async Task<EmailTemplate?> GetEmailTemplateByIdAsync(int emailTemplateId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailTemplateEntity>("Email.EmailTemplate_GetById", new
            {
                EmailTemplateId = emailTemplateId,
                OrganizationId = organizationId
            });

            var e = res?.FirstOrDefault();
            return e == null ? null : ConvertEntityToModel(e);
        }

        public async Task<IEnumerable<EmailTemplate>> GetEmailTemplatesByOrganizationIdAsync(Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailTemplateEntity>("Email.EmailTemplate_GetAllByOrganizationId", new
            {
                OrganizationId = organizationId
            });

            return (res ?? Enumerable.Empty<EmailTemplateEntity>()).Select(ConvertEntityToModel).ToList();
        }
        #endregion

        #region EmailTemplate Writes
        public async Task<EmailTemplate> CreateEmailTemplateAsync(EmailTemplate emailTemplate)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailTemplateEntity>("Email.EmailTemplate_Add", new
            {
                OrganizationId = emailTemplate.OrganizationId,
                OfficeId = emailTemplate.OfficeId,
                EmailName = emailTemplate.EmailName,
                TemplateId = emailTemplate.TemplateId
            });

            var e = res?.FirstOrDefault() ?? throw new Exception("EmailTemplate not created");
            return ConvertEntityToModel(e);
        }

        public async Task<EmailTemplate> UpdateEmailTemplateByIdAsync(EmailTemplate emailTemplate)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailTemplateEntity>("Email.EmailTemplate_UpdateById", new
            {
                EmailTemplateId = emailTemplate.EmailTemplateId,
                OrganizationId = emailTemplate.OrganizationId,
                OfficeId = emailTemplate.OfficeId,
                EmailName = emailTemplate.EmailName,
                TemplateId = emailTemplate.TemplateId
            });

            var e = res?.FirstOrDefault() ?? throw new Exception("EmailTemplate not found");
            return ConvertEntityToModel(e);
        }

        public async Task DeleteEmailTemplateByIdAsync(int emailTemplateId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Email.EmailTemplate_DeleteById", new
            {
                EmailTemplateId = emailTemplateId,
                OrganizationId = organizationId
            });
        }
        #endregion

        private static EmailTemplate ConvertEntityToModel(EmailTemplateEntity e)
        {
            return new EmailTemplate
            {
                EmailTemplateId = e.EmailTemplateId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                EmailName = e.EmailName,
                TemplateId = e.TemplateId
            };
        }
    }
}
