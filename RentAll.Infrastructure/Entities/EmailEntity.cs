namespace RentAll.Infrastructure.Entities;

public class EmailEntity
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
