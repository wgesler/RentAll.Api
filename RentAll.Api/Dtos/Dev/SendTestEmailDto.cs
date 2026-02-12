namespace RentAll.Api.Dtos.Dev;

public class SendTestEmailDto
{
	public string ToEmail { get; set; } = string.Empty;
	public string ToName { get; set; } = string.Empty;
	public string Subject { get; set; } = "RentAll Test Email";
	public string PlainTextContent { get; set; } = "This is a test email from RentAll local development.";
	public string HtmlContent { get; set; } = "<p>This is a test email from <strong>RentAll</strong> local development.</p>";

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (string.IsNullOrWhiteSpace(ToEmail))
			return (false, "ToEmail is required.");

		if (string.IsNullOrWhiteSpace(Subject))
			return (false, "Subject is required.");

		if (string.IsNullOrWhiteSpace(PlainTextContent) && string.IsNullOrWhiteSpace(HtmlContent))
			return (false, "Either PlainTextContent or HtmlContent must be provided.");

		return (true, null);
	}
}
