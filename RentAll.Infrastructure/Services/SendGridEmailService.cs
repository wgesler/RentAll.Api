using System.Text.RegularExpressions;
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
    private static readonly Regex DataImageSrcRegex = new(@"src\s*=\s*([""'])data:(image/[a-zA-Z0-9.+-]+);base64,([^""']+)\1", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IOptions<SendGridSettings> settings, ILogger<SendGridEmailService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    public async Task SendEmailAsync(string? sendGridName, EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

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

        var apiKey = await GetApiKeyFromKeyVaultAsync(sendGridName, cancellationToken);
        var client = new SendGridClient(apiKey);

        var from = ToSendGridAddress(fromEmail, fromName);
        var toRecipients = message.ToRecipients
            .Select(recipient => ToSendGridAddress(recipient.Email, recipient.Name))
            .ToList();
        var htmlContent = message.HtmlContent;
        var mail = MailHelper.CreateSingleEmailToMultipleRecipients(from, toRecipients, message.Subject,
            string.IsNullOrWhiteSpace(message.PlainTextContent) ? null : message.PlainTextContent,
            null,
            false);
        if (!string.IsNullOrWhiteSpace(htmlContent))
            mail.HtmlContent = AttachInlineImages(mail, htmlContent);
        mail.SetReplyTo(ToSendGridAddress(fromEmail, fromName));

        if (!_settings.EnableSendGridClickTracking)
            mail.SetClickTracking(false, false);

        foreach (var recipient in message.CcRecipients.Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email)))
        {
            mail.AddCc(ToSendGridAddress(recipient.Email, recipient.Name));
        }

        foreach (var recipient in message.BccRecipients.Where(recipient => !string.IsNullOrWhiteSpace(recipient.Email)))
        {
            mail.AddBcc(ToSendGridAddress(recipient.Email, recipient.Name));
        }

        AddMailAttachment(mail, message.FileDetails);
        foreach (var additionalFile in message.AdditionalFileDetails ?? [])
            AddMailAttachment(mail, additionalFile);

        var response = await client.SendEmailAsync(mail, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
        _logger.LogError("SendGrid email send failed. StatusCode: {StatusCode}; Response: {ResponseBody}", response.StatusCode, errorBody);

        throw new InvalidOperationException(
            $"SendGrid email send failed with status code {(int)response.StatusCode}. " +
            $"Response body: {errorBody}");
    }

    private static void AddMailAttachment(SendGridMessage mail, FileDetails? fileDetails)
    {
        if (fileDetails == null)
            return;

        var fileName = string.IsNullOrWhiteSpace(fileDetails.FileName) ? "attachment" : fileDetails.FileName;
        var contentType = string.IsNullOrWhiteSpace(fileDetails.ContentType)
            ? "application/octet-stream"
            : fileDetails.ContentType;

        var base64File = fileDetails.File;
        if (string.IsNullOrWhiteSpace(base64File) && !string.IsNullOrWhiteSpace(fileDetails.DataUrl))
            base64File = fileDetails.DataUrl;

        if (!string.IsNullOrWhiteSpace(base64File) && base64File.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = base64File.IndexOf(',');
            base64File = commaIndex >= 0 ? base64File[(commaIndex + 1)..] : string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(base64File))
            mail.AddAttachment(fileName, base64File, contentType);
    }

    private static string AttachInlineImages(SendGridMessage mail, string htmlContent)
    {
        var index = 0;
        return DataImageSrcRegex.Replace(htmlContent, match =>
        {
            var quote = match.Groups[1].Value;
            var contentType = match.Groups[2].Value;
            var base64 = match.Groups[3].Value;
            if (string.IsNullOrWhiteSpace(base64))
                return match.Value;

            index++;
            var contentId = $"rentall-inline-{index}";
            var subtype = contentType.Contains('/') ? contentType.Split('/')[1] : "png";
            if (subtype.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
                subtype = "jpg";

            mail.AddAttachment(new Attachment
            {
                Content = base64,
                Type = contentType,
                Filename = $"image-{index}.{subtype}",
                Disposition = "inline",
                ContentId = contentId
            });
            return $"src={quote}cid:{contentId}{quote}";
        });
    }

    private static SendGridEmailAddress ToSendGridAddress(string email, string? name)
    {
        var displayName = RentAll.Domain.Models.Common.EmailAddress.NormalizeName(name);
        return displayName is null
            ? new SendGridEmailAddress(email)
            : new SendGridEmailAddress(email, displayName);
    }

    private async Task<string> GetApiKeyFromKeyVaultAsync(string? keyVaultSecretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyVaultSecretName))
            throw new InvalidOperationException("SendGrid Key Vault secret name is not configured.");

        var kvUri = _settings.KeyVaultUri;
        if (string.IsNullOrWhiteSpace(kvUri))
            throw new InvalidOperationException("SendGridSettings:KeyVaultUri is not set.");

        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(keyVaultSecretName, cancellationToken: cancellationToken);
        return secret.Value.Value;
    }
}
