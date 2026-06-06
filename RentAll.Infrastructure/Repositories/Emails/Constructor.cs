using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;
using RentAll.Domain.Scheduling;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                ReservationId = e.ReservationId,
                ReservationCode = e.ReservationCode,
                ToRecipients = MapDomainAddresses(e.ToRecipients),
                CcRecipients = MapDomainAddresses(e.CcRecipients),
                BccRecipients = MapDomainAddresses(e.BccRecipients),
                FromRecipient = MapDomainAddress(e.FromRecipient),
                Subject = e.Subject,
                PlainTextContent = e.PlainTextContent,
                HtmlContent = e.HtmlContent,
                DocumentId = e.DocumentId,
                AttachmentName = e.AttachmentName,
                AttachmentPath = e.AttachmentPath,
                EmailType = (EmailType)e.EmailTypeId,
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

        private EmailHtml ConvertEntityToModel(EmailHtmlEntity e)
        {
            return new EmailHtml
            {
                OrganizationId = e.OrganizationId,
                WelcomeLetter = e.WelcomeLetter,
                CorporateLetter = e.CorporateLetter,
                Lease = e.Lease,
                CorporateLease = e.CorporateLease,
                Invoice = e.Invoice,
                CorporateInvoice = e.CorporateInvoice,
                LetterSubject = e.LetterSubject,
                LeaseSubject = e.LeaseSubject,
                InvoiceSubject = e.InvoiceSubject,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };
        }

        private static EmailEntity ConvertModelToEntity(Email model)
        {
            return new EmailEntity
            {
                EmailId = model.EmailId,
                OrganizationId = model.OrganizationId,
                OfficeId = model.OfficeId,
                PropertyId = model.PropertyId,
                PropertyCode = model.PropertyCode,
                ReservationId = model.ReservationId,
                ReservationCode = model.ReservationCode,
                ToRecipients = MapEntityAddresses(model.ToRecipients),
                CcRecipients = MapEntityAddresses(model.CcRecipients),
                BccRecipients = MapEntityAddresses(model.BccRecipients),
                FromRecipient = MapEntityAddress(model.FromRecipient),
                Subject = model.Subject,
                PlainTextContent = model.PlainTextContent,
                HtmlContent = model.HtmlContent,
                DocumentId = model.DocumentId,
                AttachmentName = model.AttachmentName,
                AttachmentPath = model.AttachmentPath,
                EmailTypeId = (int)model.EmailType,
                EmailStatusId = (int)model.EmailStatus,
                AttemptCount = model.AttemptCount,
                LastError = model.LastError,
                LastAttemptedOn = model.LastAttemptedOn,
                SentOn = model.SentOn,
                CreatedOn = model.CreatedOn,
                CreatedBy = model.CreatedBy,
                ModifiedOn = model.ModifiedOn,
                ModifiedBy = model.ModifiedBy
            };
        }

        private static EmailEntity ConvertStoredProcRowToEntity(EmailStoredProcRow row)
        {
            return new EmailEntity
            {
                EmailId = row.EmailId,
                OrganizationId = row.OrganizationId,
                OfficeId = row.OfficeId,
                PropertyId = row.PropertyId,
                PropertyCode = row.PropertyCode,
                ReservationId = row.ReservationId,
                ReservationCode = row.ReservationCode,
                ToRecipients = DeserializeRecipients(row.ToRecipients),
                CcRecipients = DeserializeRecipients(row.CcRecipients),
                BccRecipients = DeserializeRecipients(row.BccRecipients),
                FromRecipient = DeserializeRecipient(row.FromRecipient),
                Subject = row.Subject,
                PlainTextContent = row.PlainTextContent,
                HtmlContent = row.HtmlContent,
                DocumentId = row.DocumentId,
                AttachmentName = row.AttachmentName,
                AttachmentPath = row.AttachmentPath,
                EmailTypeId = row.EmailTypeId,
                EmailStatusId = row.EmailStatusId,
                AttemptCount = row.AttemptCount,
                LastError = row.LastError,
                LastAttemptedOn = row.LastAttemptedOn,
                SentOn = row.SentOn,
                CreatedOn = row.CreatedOn,
                CreatedBy = row.CreatedBy,
                ModifiedOn = row.ModifiedOn,
                ModifiedBy = row.ModifiedBy
            };
        }

        private Email ConvertStoredProcRowToModel(EmailStoredProcRow row)
        {
            return ConvertEntityToModel(ConvertStoredProcRowToEntity(row));
        }

        private Alert ConvertEntityToModel(AlertEntity e)
        {
            var alert = new Alert
            {
                AlertId = e.AlertId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                ReservationId = e.ReservationId,
                ReservationCode = e.ReservationCode,
                TicketId = e.TicketId,
                ArrivalDate = e.ArrivalDate,
                DepartureDate = e.DepartureDate,
                ToRecipients = MapDomainAddresses(e.ToRecipients),
                CcRecipients = MapDomainAddresses(e.CcRecipients),
                BccRecipients = MapDomainAddresses(e.BccRecipients),
                FromRecipient = MapDomainAddress(e.FromRecipient),
                Subject = e.Subject,
                PlainTextContent = e.PlainTextContent,
                EmailType = (EmailType)e.EmailTypeId,
                StartDate = e.StartDate,
                DaysBeforeDeparture = e.DaysBeforeDeparture,
                Frequency = (FrequencyType)e.FrequencyId,
                EmailStatus = (EmailStatus)e.EmailStatusId,
                AttemptCount = e.AttemptCount,
                LastError = e.LastError,
                LastAttemptedOn = e.LastAttemptedOn,
                SentOn = e.SentOn,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            alert.NextAlertDate = AlertScheduleEvaluator.GetNextAlertDate(alert, DateTimeOffset.UtcNow);
            return alert;
        }

        private static AlertEntity ConvertModelToEntity(Alert model)
        {
            return new AlertEntity
            {
                AlertId = model.AlertId,
                OrganizationId = model.OrganizationId,
                OfficeId = model.OfficeId,
                PropertyId = model.PropertyId,
                PropertyCode = model.PropertyCode,
                ReservationId = model.ReservationId,
                ReservationCode = model.ReservationCode,
                TicketId = model.TicketId,
                ToRecipients = MapEntityAddresses(model.ToRecipients),
                CcRecipients = MapEntityAddresses(model.CcRecipients),
                BccRecipients = MapEntityAddresses(model.BccRecipients),
                FromRecipient = MapEntityAddress(model.FromRecipient),
                Subject = model.Subject,
                PlainTextContent = model.PlainTextContent,
                EmailTypeId = (int)model.EmailType,
                StartDate = model.StartDate,
                DaysBeforeDeparture = model.DaysBeforeDeparture,
                FrequencyId = (int)model.Frequency,
                EmailStatusId = (int)model.EmailStatus,
                AttemptCount = model.AttemptCount,
                LastError = model.LastError,
                LastAttemptedOn = model.LastAttemptedOn,
                SentOn = model.SentOn,
                IsActive = model.IsActive,
                CreatedOn = model.CreatedOn,
                CreatedBy = model.CreatedBy,
                ModifiedOn = model.ModifiedOn,
                ModifiedBy = model.ModifiedBy
            };
        }

        private static AlertEntity ConvertStoredProcRowToEntity(AlertStoredProcRow row)
        {
            return new AlertEntity
            {
                AlertId = row.AlertId,
                OrganizationId = row.OrganizationId,
                OfficeId = row.OfficeId,
                PropertyId = row.PropertyId,
                PropertyCode = row.PropertyCode,
                ReservationId = row.ReservationId,
                ReservationCode = row.ReservationCode,
                TicketId = row.TicketId,
                ArrivalDate = row.ArrivalDate,
                DepartureDate = row.DepartureDate,
                ToRecipients = DeserializeRecipients(row.ToRecipients),
                CcRecipients = DeserializeRecipients(row.CcRecipients),
                BccRecipients = DeserializeRecipients(row.BccRecipients),
                FromRecipient = DeserializeRecipient(row.FromRecipient),
                Subject = row.Subject,
                PlainTextContent = row.PlainTextContent,
                EmailTypeId = row.EmailTypeId,
                StartDate = row.StartDate,
                DaysBeforeDeparture = row.DaysBeforeDeparture,
                FrequencyId = row.FrequencyId,
                EmailStatusId = row.EmailStatusId,
                AttemptCount = row.AttemptCount,
                LastError = row.LastError,
                LastAttemptedOn = row.LastAttemptedOn,
                SentOn = row.SentOn,
                IsActive = row.IsActive,
                CreatedOn = row.CreatedOn,
                CreatedBy = row.CreatedBy,
                ModifiedOn = row.ModifiedOn,
                ModifiedBy = row.ModifiedBy
            };
        }

        private Alert ConvertStoredProcRowToModel(AlertStoredProcRow row)
        {
            return ConvertEntityToModel(ConvertStoredProcRowToEntity(row));
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static EmailAddress MapDomainAddress(EmailAddressEntity? entity)
        {
            if (entity is null)
                return EmailAddress.Create(string.Empty, null);

            return EmailAddress.Create(entity.Email, entity.Name);
        }

        private static List<EmailAddress> MapDomainAddresses(IEnumerable<EmailAddressEntity>? entities)
        {
            return (entities ?? []).Select(MapDomainAddress).ToList();
        }

        private static EmailAddressEntity MapEntityAddress(EmailAddress? address)
        {
            if (address is null)
                return new EmailAddressEntity { Email = string.Empty, Name = null };

            return new EmailAddressEntity
            {
                Email = address.Email,
                Name = EmailAddress.NormalizeName(address.Name)
            };
        }

        private static List<EmailAddressEntity> MapEntityAddresses(IEnumerable<EmailAddress>? addresses)
        {
            return (addresses ?? []).Select(MapEntityAddress).ToList();
        }

        private static EmailAddressEntity NormalizeEntity(EmailAddressEntity entity)
        {
            entity.Name = EmailAddress.NormalizeName(entity.Name);
            return entity;
        }

        private static List<EmailAddressEntity> DeserializeRecipients(string recipientsJson)
        {
            if (string.IsNullOrWhiteSpace(recipientsJson))
                return [];

            try
            {
                var recipients = JsonSerializer.Deserialize<List<EmailAddressEntity>>(recipientsJson, SerializerOptions) ?? [];
                return recipients.Select(NormalizeEntity).ToList();
            }
            catch
            {
                return [];
            }
        }

        private static EmailAddressEntity DeserializeRecipient(string recipientJson)
        {
            if (string.IsNullOrWhiteSpace(recipientJson))
                return new EmailAddressEntity();

            try
            {
                var value = JsonSerializer.Deserialize<EmailAddressEntity>(recipientJson, SerializerOptions);
                return NormalizeEntity(value ?? new EmailAddressEntity());
            }
            catch
            {
                return new EmailAddressEntity();
            }
        }

        private static string SerializeRecipients(IEnumerable<EmailAddressEntity>? recipients)
        {
            var normalized = (recipients ?? []).Select(NormalizeEntity).ToList();
            return JsonSerializer.Serialize(normalized, SerializerOptions);
        }

        private static string SerializeRecipient(EmailAddressEntity? recipient)
        {
            return JsonSerializer.Serialize(NormalizeEntity(recipient ?? new EmailAddressEntity()), SerializerOptions);
        }

        private sealed class EmailStoredProcRow
        {
            public Guid EmailId { get; set; }
            public Guid OrganizationId { get; set; }
            public int OfficeId { get; set; }
            public Guid? PropertyId { get; set; }
            public string? PropertyCode { get; set; }
            public Guid? ReservationId { get; set; }
            public string? ReservationCode { get; set; }
            public string ToRecipients { get; set; } = "[]";
            public string CcRecipients { get; set; } = "[]";
            public string BccRecipients { get; set; } = "[]";
            public string FromRecipient { get; set; } = "{}";
            public string Subject { get; set; } = string.Empty;
            public string PlainTextContent { get; set; } = string.Empty;
            public string HtmlContent { get; set; } = string.Empty;
            public Guid? DocumentId { get; set; }
            public string AttachmentName { get; set; } = string.Empty;
            public string AttachmentPath { get; set; } = string.Empty;
            public int EmailTypeId { get; set; }
            public int EmailStatusId { get; set; }
            public int AttemptCount { get; set; }
            public string LastError { get; set; } = string.Empty;
            public DateTimeOffset? LastAttemptedOn { get; set; }
            public DateTimeOffset? SentOn { get; set; }
            public DateTimeOffset CreatedOn { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTimeOffset ModifiedOn { get; set; }
            public Guid ModifiedBy { get; set; }
        }

        private sealed class AlertStoredProcRow
        {
            public Guid AlertId { get; set; }
            public Guid OrganizationId { get; set; }
            public int OfficeId { get; set; }
            public Guid? PropertyId { get; set; }
            public string? PropertyCode { get; set; }
            public Guid? ReservationId { get; set; }
            public string? ReservationCode { get; set; }
            public Guid? TicketId { get; set; }
            public DateOnly? ArrivalDate { get; set; }
            public DateOnly? DepartureDate { get; set; }
            public string ToRecipients { get; set; } = "[]";
            public string CcRecipients { get; set; } = "[]";
            public string BccRecipients { get; set; } = "[]";
            public string FromRecipient { get; set; } = "{}";
            public string Subject { get; set; } = string.Empty;
            public string PlainTextContent { get; set; } = string.Empty;
            public int EmailTypeId { get; set; }
            public DateOnly? StartDate { get; set; }
            public int? DaysBeforeDeparture { get; set; }
            public int FrequencyId { get; set; }
            public int EmailStatusId { get; set; }
            public int AttemptCount { get; set; }
            public string LastError { get; set; } = string.Empty;
            public DateTimeOffset? LastAttemptedOn { get; set; }
            public DateTimeOffset? SentOn { get; set; }
            public bool IsActive { get; set; }
            public DateTimeOffset CreatedOn { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTimeOffset ModifiedOn { get; set; }
            public Guid ModifiedBy { get; set; }
        }
    }
}
