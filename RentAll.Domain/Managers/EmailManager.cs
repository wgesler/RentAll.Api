using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
using System.Reflection;

namespace RentAll.Domain.Managers;

public class EmailManager : IEmailManager
{
	private const int MaxRetryAttempts = 3;

	private readonly IEmailRepository _emailRepository;
	private readonly IEmailService _emailService;
	private readonly IFileService _fileService;
	private readonly IDocumentRepository _documentRepository;

	public EmailManager(
		IEmailRepository emailRepository,
		IEmailService emailService,
		IFileService fileService,
		IDocumentRepository documentRepository)
	{
		_emailRepository = emailRepository;
		_emailService = emailService;
		_fileService = fileService;
		_documentRepository = documentRepository;
	}

	public async Task<Email> SendEmail(Email email)
	{
		// Translate the email into SendGrid format
		var originalEmailMessage = email.ToEmailMessage();

		// If there's an attachment, store it in blob storage
		if (email.FileDetails != null)
		{
			var documentPath = await _fileService.SaveDocumentAsync(email.OrganizationId, email.OfficeId, email.FileDetails.File, email.FileDetails.FileName, email.FileDetails.ContentType, DocumentType.Attachment);
			email.AttachmentPath = documentPath;
		}

        // Save this email in our database
        var createdEmail = await _emailRepository.CreateAsync(email);
		if (createdEmail.EmailStatus != EmailStatus.Attempting)
			return createdEmail;

		// Preserve actor for audit updates
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
