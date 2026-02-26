using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Emails
{
    public partial class EmailRepository
    {
        #region Create
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
        #endregion

        #region Select
        public async Task<IEnumerable<Email>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailStoredProcRow>("Email.Email_GetAllByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Email>();

            return res.Select(ConvertStoredProcRowToModel);
        }

        public async Task<Email?> GetByIdAsync(Guid emailId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailStoredProcRow>("Email.Email_GetById", new
            {
                EmailId = emailId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Update
        public async Task<Email> UpdateByIdAsync(Email email)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<EmailStoredProcRow>("Email.Email_UpdateById", new
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

            return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
        }
        #endregion
    }
}
