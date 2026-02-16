using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Models.Common;
using RentAll.Infrastructure.Services;

namespace RentAll.Test;

public class SendGridSystemTests
{
	[Fact]
	public async Task SendGrid_Should_Send_Real_Email_To_User()
	{
		var settings = LoadSendGridSettings();
		var service = new SendGridEmailService(Options.Create(settings), NullLogger<SendGridEmailService>.Instance);

		var subject = $"RentAll system test {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
		var message = new EmailMessage
		{
			ToRecipients =
			[
				new EmailAddress
				{
					Email = "w.gesler@gmail.com",
					Name = "Will Gesler"
				}
			],
			Subject = subject,
			PlainTextContent = "This is a real SendGrid system test email from RentAll.Test.",
			HtmlContent = $"<p>This is a <strong>real SendGrid system test</strong> email from RentAll.Test.</p><p>Subject: {subject}</p>"
		};

		// If SendGrid rejects the request, SendGridEmailService throws and this test fails.
		await service.SendEmailAsync(message, CancellationToken.None);
	}

	private static SendGridSettings LoadSendGridSettings()
	{
		var settings = new SendGridSettings
		{
			ApiKey = Environment.GetEnvironmentVariable("SendGridSettings__ApiKey") ?? string.Empty,
			FromEmail = Environment.GetEnvironmentVariable("SendGridSettings__FromEmail") ?? string.Empty,
			FromName = Environment.GetEnvironmentVariable("SendGridSettings__FromName") ?? string.Empty
		};

		if (!string.IsNullOrWhiteSpace(settings.ApiKey) &&
			!string.IsNullOrWhiteSpace(settings.FromEmail))
		{
			return settings;
		}

		var appSettingsPath = GetDevelopmentSettingsPath();
		if (!File.Exists(appSettingsPath))
			throw new InvalidOperationException($"Could not find appsettings.Development.json at '{appSettingsPath}'.");

		using var file = File.OpenRead(appSettingsPath);
		using var json = JsonDocument.Parse(file);

		if (!json.RootElement.TryGetProperty("SendGridSettings", out var sg))
			throw new InvalidOperationException("SendGridSettings section is missing in appsettings.Development.json.");

		settings.ApiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
			? (sg.TryGetProperty("ApiKey", out var apiKey) ? apiKey.GetString() ?? string.Empty : string.Empty)
			: settings.ApiKey;

		settings.FromEmail = string.IsNullOrWhiteSpace(settings.FromEmail)
			? (sg.TryGetProperty("FromEmail", out var fromEmail) ? fromEmail.GetString() ?? string.Empty : string.Empty)
			: settings.FromEmail;

		settings.FromName = string.IsNullOrWhiteSpace(settings.FromName)
			? (sg.TryGetProperty("FromName", out var fromName) ? fromName.GetString() ?? string.Empty : string.Empty)
			: settings.FromName;

		if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.FromEmail))
		{
			throw new InvalidOperationException(
				"SendGrid settings are not configured. Set SendGridSettings.ApiKey and SendGridSettings.FromEmail " +
				"in appsettings.Development.json or environment variables.");
		}

		return settings;
	}

	private static string GetDevelopmentSettingsPath()
	{
		var candidates = new[]
		{
			// Common when running from solution root.
			Path.Combine(Directory.GetCurrentDirectory(), "RentAll.Api", "appsettings.Development.json"),
			Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"),

			// Common when running from test output folder.
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../RentAll.Api/appsettings.Development.json")),
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../appsettings.Development.json")),
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../RentAll.Api/appsettings.Development.json")),
			Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../appsettings.Development.json"))
		};

		var existingPath = candidates.FirstOrDefault(File.Exists);
		return existingPath ?? candidates[0];
	}
}
