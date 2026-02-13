using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class EmailManager : IEmailManager
{
	private const int MaxRetryAttempts = 3;

	private readonly IEmailRepository _emailRepository;
	private readonly IEmailService _emailService;

	public EmailManager(
		IEmailRepository emailRepository,
		IEmailService emailService)
	{
		_emailRepository = emailRepository;
		_emailService = emailService;
	}

	public async Task<Email> SendEmail(Email email)
	{
		var originalEmailMessage = email.ToEmailMessage();
		var createdEmail = await _emailRepository.CreateAsync(email);
		if (createdEmail.EmailStatus != EmailStatus.Attempting)
			return createdEmail;

		// Preserve actor for audit updates.
		var modifiedBy = createdEmail.CreatedBy != Guid.Empty ? createdEmail.CreatedBy : email.CreatedBy;
		var currentEmail = createdEmail;

		for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
		{
			currentEmail.EmailStatus = EmailStatus.Attempting;
			currentEmail.AttemptCount = attempt;
			currentEmail.LastAttemptedOn = DateTimeOffset.UtcNow;
			currentEmail.LastError = $"Attempt {attempt} of {MaxRetryAttempts}";
			currentEmail.ModifiedBy = modifiedBy;
			currentEmail = await _emailRepository.UpdateByIdAsync(currentEmail);

			try
			{
				await _emailService.SendEmailAsync(originalEmailMessage);
				currentEmail.EmailStatus = EmailStatus.Succeeded;
				currentEmail.SentOn = DateTimeOffset.UtcNow;
				currentEmail.LastError = "Accepted by SendGrid.";
				currentEmail.ModifiedBy = modifiedBy;
				return await _emailRepository.UpdateByIdAsync(currentEmail);
			}
			catch (Exception ex)
			{
				currentEmail.EmailStatus = EmailStatus.Failed;
				currentEmail.LastError = ex.Message;
				currentEmail.ModifiedBy = modifiedBy;
				currentEmail = await _emailRepository.UpdateByIdAsync(currentEmail);

				if (!IsUnreachable(ex) || attempt >= MaxRetryAttempts)
					return currentEmail;

				await Task.Delay(TimeSpan.FromSeconds(attempt));
			}
		}

		return currentEmail;
	}

	private static bool IsUnreachable(Exception ex)
	{
		var current = ex;
		while (current != null)
		{
			if (current is HttpRequestException || current is TaskCanceledException || current is TimeoutException)
				return true;

			current = current.InnerException!;
		}

		return false;
	}
}
