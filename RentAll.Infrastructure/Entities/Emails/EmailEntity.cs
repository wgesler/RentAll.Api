namespace RentAll.Infrastructure.Entities.Emails;

public class EmailEntity
{
    public Guid EmailId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public List<EmailAddressEntity> ToRecipients { get; set; } = [];
    public List<EmailAddressEntity> CcRecipients { get; set; } = [];
    public List<EmailAddressEntity> BccRecipients { get; set; } = [];
    public EmailAddressEntity FromRecipient { get; set; } = new();
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
