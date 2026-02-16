using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Emails;

public class EmailResponseDto
{
	public Guid EmailId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ReservationId { get; set; }
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

	public EmailResponseDto(Email email)
	{
		EmailId = email.EmailId;
		OrganizationId = email.OrganizationId;
		OfficeId = email.OfficeId;
		PropertyId = email.PropertyId;
		ReservationId = email.ReservationId;
		ToRecipients = email.ToRecipients;
		CcRecipients = email.CcRecipients;
		BccRecipients = email.BccRecipients;
		FromRecipient = email.FromRecipient;
		Subject = email.Subject;
		PlainTextContent = email.PlainTextContent;
		HtmlContent = email.HtmlContent;
		DocumentId = email.DocumentId;
		AttachmentName = email.AttachmentName;
		AttachmentPath = email.AttachmentPath;
		FileDetails = email.FileDetails;
		EmailTypeId = (int)email.EmailType;
		EmailStatusId = (int)email.EmailStatus;
		AttemptCount = email.AttemptCount;
		LastError = email.LastError;
		LastAttemptedOn = email.LastAttemptedOn;
		SentOn = email.SentOn;
		CreatedOn = email.CreatedOn;
		CreatedBy = email.CreatedBy;
		ModifiedOn = email.ModifiedOn;
		ModifiedBy = email.ModifiedBy;
	}
}
