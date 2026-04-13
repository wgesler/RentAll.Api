using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Entities.Emails;
using System.Text.Json;

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
                ReservationId = e.ReservationId,
                ToRecipients = (e.ToRecipients ?? [])
                    .Select(recipient => new Domain.Models.Common.EmailAddress
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                CcRecipients = (e.CcRecipients ?? [])
                    .Select(recipient => new Domain.Models.Common.EmailAddress
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                BccRecipients = (e.BccRecipients ?? [])
                    .Select(recipient => new Domain.Models.Common.EmailAddress
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                FromRecipient = new Domain.Models.Common.EmailAddress
                {
                    Email = e.FromRecipient?.Email ?? string.Empty,
                    Name = e.FromRecipient?.Name ?? string.Empty
                },
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
                ReservationId = model.ReservationId,
                ToRecipients = (model.ToRecipients ?? [])
                    .Select(recipient => new EmailAddressEntity
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                CcRecipients = (model.CcRecipients ?? [])
                    .Select(recipient => new EmailAddressEntity
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                BccRecipients = (model.BccRecipients ?? [])
                    .Select(recipient => new EmailAddressEntity
                    {
                        Email = recipient.Email,
                        Name = recipient.Name
                    })
                    .ToList(),
                FromRecipient = new EmailAddressEntity
                {
                    Email = model.FromRecipient?.Email ?? string.Empty,
                    Name = model.FromRecipient?.Name ?? string.Empty
                },
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
                ReservationId = row.ReservationId,
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
            return new Alert
            {
                AlertId = e.AlertId,
                OrganizationId = e.OrganizationId,
                OfficeId = e.OfficeId,
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                ReservationId = e.ReservationId,
                ReservationCode = e.ReservationCode,
                ToRecipients = (e.ToRecipients ?? [])
                    .Select(r => new Domain.Models.Common.EmailAddress { Email = r.Email, Name = r.Name })
                    .ToList(),
                CcRecipients = (e.CcRecipients ?? [])
                    .Select(r => new Domain.Models.Common.EmailAddress { Email = r.Email, Name = r.Name })
                    .ToList(),
                BccRecipients = (e.BccRecipients ?? [])
                    .Select(r => new Domain.Models.Common.EmailAddress { Email = r.Email, Name = r.Name })
                    .ToList(),
                FromRecipient = new Domain.Models.Common.EmailAddress
                {
                    Email = e.FromRecipient?.Email ?? string.Empty,
                    Name = e.FromRecipient?.Name ?? string.Empty
                },
                Subject = e.Subject,
                PlainTextContent = e.PlainTextContent,
                EmailType = (EmailType)e.EmailTypeId,
                StartDate = e.StartDate,
                Frequency = (FrequencyType)e.FrequencyId,
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
                ToRecipients = (model.ToRecipients ?? [])
                    .Select(r => new EmailAddressEntity { Email = r.Email, Name = r.Name })
                    .ToList(),
                CcRecipients = (model.CcRecipients ?? [])
                    .Select(r => new EmailAddressEntity { Email = r.Email, Name = r.Name })
                    .ToList(),
                BccRecipients = (model.BccRecipients ?? [])
                    .Select(r => new EmailAddressEntity { Email = r.Email, Name = r.Name })
                    .ToList(),
                FromRecipient = new EmailAddressEntity
                {
                    Email = model.FromRecipient?.Email ?? string.Empty,
                    Name = model.FromRecipient?.Name ?? string.Empty
                },
                Subject = model.Subject,
                PlainTextContent = model.PlainTextContent,
                EmailTypeId = (int)model.EmailType,
                StartDate = model.StartDate,
                FrequencyId = (int)model.Frequency,
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
                ToRecipients = DeserializeRecipients(row.ToRecipients),
                CcRecipients = DeserializeRecipients(row.CcRecipients),
                BccRecipients = DeserializeRecipients(row.BccRecipients),
                FromRecipient = DeserializeRecipient(row.FromRecipient),
                Subject = row.Subject,
                PlainTextContent = row.PlainTextContent,
                EmailTypeId = row.EmailTypeId,
                StartDate = row.StartDate,
                FrequencyId = row.FrequencyId,
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

        private Alert ConvertStoredProcRowToModel(AlertStoredProcRow row)
        {
            return ConvertEntityToModel(ConvertStoredProcRowToEntity(row));
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static List<EmailAddressEntity> DeserializeRecipients(string recipientsJson)
        {
            if (string.IsNullOrWhiteSpace(recipientsJson))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<EmailAddressEntity>>(recipientsJson, SerializerOptions) ?? [];
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
                return value ?? new EmailAddressEntity();
            }
            catch
            {
                return new EmailAddressEntity();
            }
        }

        private static string SerializeRecipients(IEnumerable<EmailAddressEntity>? recipients)
        {
            return JsonSerializer.Serialize(recipients ?? Enumerable.Empty<EmailAddressEntity>(), SerializerOptions);
        }

        private static string SerializeRecipient(EmailAddressEntity? recipient)
        {
            return JsonSerializer.Serialize(recipient ?? new EmailAddressEntity(), SerializerOptions);
        }

        private sealed class EmailStoredProcRow
        {
            public Guid EmailId { get; set; }
            public Guid OrganizationId { get; set; }
            public int OfficeId { get; set; }
            public Guid? PropertyId { get; set; }
            public Guid? ReservationId { get; set; }
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
            public string ToRecipients { get; set; } = "[]";
            public string CcRecipients { get; set; } = "[]";
            public string BccRecipients { get; set; } = "[]";
            public string FromRecipient { get; set; } = "{}";
            public string Subject { get; set; } = string.Empty;
            public string PlainTextContent { get; set; } = string.Empty;
            public int EmailTypeId { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public int FrequencyId { get; set; }
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
    }
}
