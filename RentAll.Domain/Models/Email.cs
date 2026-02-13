using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Email
{
	public Guid EmailId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string ToEmail { get; set; } = string.Empty;
	public string ToName { get; set; } = string.Empty;
	public string FromEmail { get; set; } = string.Empty;
	public string FromName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string PlainTextContent { get; set; } = string.Empty;
	public string HtmlContent { get; set; } = string.Empty;
	public string AttachmentPath { get; set; } = string.Empty;
	public FileDetails? FileDetails { get; set; }
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
			FromEmail = FromEmail,
			FromName = FromName,
			ToEmail = ToEmail,
			ToName = ToName,
			Subject = Subject,
			PlainTextContent = PlainTextContent,
			HtmlContent = HtmlContent,
			FileDetails = FileDetails
		};
	}
}
