using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RentAll.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
	private readonly SendGridSettings _settings;
	private readonly ILogger<SendGridEmailService> _logger;

	public SendGridEmailService(IOptions<SendGridSettings> settings, ILogger<SendGridEmailService> logger)
	{
		_settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
		_logger = logger;
	}

	public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(message);

		if (string.IsNullOrWhiteSpace(_settings.ApiKey))
			throw new InvalidOperationException("SendGridSettings:ApiKey is not configured.");

		if (string.IsNullOrWhiteSpace(message.ToEmail))
			throw new ArgumentException("ToEmail is required.", nameof(message));

		if (string.IsNullOrWhiteSpace(message.Subject))
			throw new ArgumentException("Subject is required.", nameof(message));

		if (string.IsNullOrWhiteSpace(message.PlainTextContent) && string.IsNullOrWhiteSpace(message.HtmlContent))
			throw new ArgumentException("Either PlainTextContent or HtmlContent must be provided.", nameof(message));

		var fromEmail = string.IsNullOrWhiteSpace(message.FromEmail) ? _settings.FromEmail : message.FromEmail;
		var fromName = string.IsNullOrWhiteSpace(message.FromName) ? _settings.FromName : message.FromName;
		if (string.IsNullOrWhiteSpace(fromEmail))
			throw new InvalidOperationException("FromEmail is required either in EmailMessage or SendGridSettings.");

		var client = new SendGridClient(_settings.ApiKey);

		var from = new EmailAddress(fromEmail, fromName);
		var to = new EmailAddress(message.ToEmail, message.ToName);
		var mail = MailHelper.CreateSingleEmail(from, to, message.Subject,
			string.IsNullOrWhiteSpace(message.PlainTextContent) ? null : message.PlainTextContent,
			string.IsNullOrWhiteSpace(message.HtmlContent) ? null : message.HtmlContent);

		if (message.FileDetails != null)
		{
			var fileName = string.IsNullOrWhiteSpace(message.FileDetails.FileName) ? "attachment" : message.FileDetails.FileName;
			var contentType = string.IsNullOrWhiteSpace(message.FileDetails.ContentType)
				? "application/octet-stream"
				: message.FileDetails.ContentType;

			var base64File = message.FileDetails.File;
			if (string.IsNullOrWhiteSpace(base64File) && !string.IsNullOrWhiteSpace(message.FileDetails.DataUrl))
			{
				base64File = message.FileDetails.DataUrl;
			}

			if (base64File.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
			{
				var commaIndex = base64File.IndexOf(',');
				base64File = commaIndex >= 0 ? base64File[(commaIndex + 1)..] : string.Empty;
			}

			if (!string.IsNullOrWhiteSpace(base64File))
			{
				mail.AddAttachment(fileName, base64File, contentType);
			}
		}

		var response = await client.SendEmailAsync(mail, cancellationToken);
		if (response.IsSuccessStatusCode)
			return;

		var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
         _logger.LogError("SendGrid email send failed. StatusCode: {StatusCode}; Response: {ResponseBody}", response.StatusCode, errorBody);

		throw new InvalidOperationException($"SendGrid email send failed with status code {(int)response.StatusCode}.");
	}
}
