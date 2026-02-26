using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Common;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGridEmailAddress = SendGrid.Helpers.Mail.EmailAddress;

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

    public async Task SendEmailAsync(Guid? organizationId, EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("SendGridSettings:ApiKey is not configured.");

        if (message.ToRecipients.Count == 0 || message.ToRecipients.Any(recipient => string.IsNullOrWhiteSpace(recipient.Email)))
            throw new ArgumentException("At least one valid ToRecipient is required.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Subject is required.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.PlainTextContent) && string.IsNullOrWhiteSpace(message.HtmlContent))
            throw new ArgumentException("Either PlainTextContent or HtmlContent must be provided.", nameof(message));

        var fromEmail = string.IsNullOrWhiteSpace(message.FromRecipient.Email) ? _settings.FromEmail : message.FromRecipient.Email;
        var fromName = string.IsNullOrWhiteSpace(message.FromRecipient.Name) ? _settings.FromName : message.FromRecipient.Name;
        if (string.IsNullOrWhiteSpace(fromEmail))
            throw new InvalidOperationException("FromRecipient.Email is required either in EmailMessage or SendGridSettings.");

        var client = new SendGridClient(_settings.ApiKey);

        var from = new SendGridEmailAddress(fromEmail, fromName);
        var toRecipients = message.ToRecipients.Select(recipient => new SendGridEmailAddress(recipient.Email, recipient.Name)).ToList();
        var mail = MailHelper.CreateSingleEmailToMultipleRecipients(from, toRecipients, message.Subject,
            string.IsNullOrWhiteSpace(message.PlainTextContent) ? null : message.PlainTextContent,
            string.IsNullOrWhiteSpace(message.HtmlContent) ? null : message.HtmlContent,
            false);
        mail.SetReplyTo(new SendGridEmailAddress(fromEmail, fromName));

        foreach (var recipient in message.CcRecipients.Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email)))
        {
            mail.AddCc(new SendGridEmailAddress(recipient.Email, recipient.Name));
        }

        foreach (var recipient in message.BccRecipients.Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email)))
        {
            mail.AddBcc(new SendGridEmailAddress(recipient.Email, recipient.Name));
        }

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

        throw new InvalidOperationException(
            $"SendGrid email send failed with status code {(int)response.StatusCode}. " +
            $"Response body: {errorBody}");
    }
}
