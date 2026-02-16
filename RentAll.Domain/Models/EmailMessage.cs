namespace RentAll.Domain.Models.Common;

public class EmailMessage
{
	public EmailAddress FromRecipient { get; set; } = new();
	public List<EmailAddress> ToRecipients { get; set; } = [];
	public List<EmailAddress> CcRecipients { get; set; } = [];
	public List<EmailAddress> BccRecipients { get; set; } = [];
	public string Subject { get; set; } = string.Empty;
	public string PlainTextContent { get; set; } = string.Empty;
	public string HtmlContent { get; set; } = string.Empty;
	public FileDetails? FileDetails { get; set; }
}
