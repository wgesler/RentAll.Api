using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Emails
{
    public partial class EmailRepository
    {
        #region Selects
        public async Task<IEnumerable<Alert>> GetAlertsByOfficeIdsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AlertStoredProcRow>("Email.Alert_GetAllByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<Alert>();

            return res.Select(ConvertStoredProcRowToModel);
        }

        public async Task<Alert?> GetAlertByIdAsync(Guid alertId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AlertStoredProcRow>("Email.Alert_GetById", new
            {
                AlertId = alertId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Creates
        public async Task<Alert> CreateAlertAsync(Alert alert)
        {
            var entity = ConvertModelToEntity(alert);
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AlertStoredProcRow>("Email.Alert_Add", new
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
                EmailTypeId = entity.EmailTypeId,
                StartDate = entity.StartDate,
                FrequencyId = entity.FrequencyId,
                EmailStatusId = entity.EmailStatusId,
                AttemptCount = entity.AttemptCount,
                LastError = entity.LastError,
                LastAttemptedOn = entity.LastAttemptedOn,
                SentOn = entity.SentOn,
                CreatedBy = entity.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Alert not created");

            return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<Alert> UpdateAlertByIdAsync(Alert alert)
        {
            var entity = ConvertModelToEntity(alert);
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<AlertStoredProcRow>("Email.Alert_UpdateById", new
            {
                AlertId = entity.AlertId,
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
                EmailTypeId = entity.EmailTypeId,
                StartDate = entity.StartDate,
                FrequencyId = entity.FrequencyId,
                EmailStatusId = entity.EmailStatusId,
                AttemptCount = entity.AttemptCount,
                LastError = entity.LastError,
                LastAttemptedOn = entity.LastAttemptedOn,
                SentOn = entity.SentOn,
                ModifiedBy = entity.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Alert not found");

            return ConvertStoredProcRowToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeleteAlertByIdAsync(Guid alertId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Email.Alert_DeleteById", new
            {
                AlertId = alertId,
                OrganizationId = organizationId
            });
        }
        #endregion
    }
}
