using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Email
{
    public Guid EmailId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public List<EmailAddress> ToRecipients { get; set; } = [];
    public List<EmailAddress> CcRecipients { get; set; } = [];
    public List<EmailAddress> BccRecipients { get; set; } = [];
    public EmailAddress FromRecipient { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public string AttachmentName { get; set; } = string.Empty;
    public string AttachmentPath { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }
    public EmailType EmailType { get; set; } = EmailType.Other;
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Unsent;
    public int AttemptCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTimeOffset? LastAttemptedOn { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public EmailMessage ToEmailMessage()
    {
        return new EmailMessage
        {
            FromRecipient = new EmailAddress
            {
                Email = FromRecipient.Email,
                Name = FromRecipient.Name
            },
            ToRecipients = ToRecipients
                .Select(recipient => new EmailAddress
                {
                    Email = recipient.Email,
                    Name = recipient.Name
                })
                .ToList(),
            CcRecipients = CcRecipients
                .Select(recipient => new EmailAddress
                {
                    Email = recipient.Email,
                    Name = recipient.Name
                })
                .ToList(),
            BccRecipients = BccRecipients
                .Select(recipient => new EmailAddress
                {
                    Email = recipient.Email,
                    Name = recipient.Name
                })
                .ToList(),
            Subject = Subject,
            PlainTextContent = PlainTextContent,
            HtmlContent = HtmlContent,
            FileDetails = FileDetails
        };
    }
}
