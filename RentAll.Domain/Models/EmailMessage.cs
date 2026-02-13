namespace RentAll.Domain.Models.Common;

public class EmailMessage
{
	public string FromEmail { get; set; } = string.Empty;
	public string FromName { get; set; } = string.Empty;
	public string ToEmail { get; set; } = string.Empty;
	public string ToName { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string PlainTextContent { get; set; } = string.Empty;
	public string HtmlContent { get; set; } = string.Empty;
	public FileDetails? FileDetails { get; set; }
}
