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
    private readonly IFileService _fileService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public EmailManager(
        IEmailRepository emailRepository,
        IEmailService emailService,
        IFileService fileService,
        IDocumentRepository documentRepository,
        IOrganizationRepository organziationRepository)
    {
        _emailRepository = emailRepository;
        _emailService = emailService;
        _fileService = fileService;
        _documentRepository = documentRepository;
        _organizationRepository = organziationRepository;
    }

    public async Task<Email> SendEmail(string? sendGridName, Email email)
    {
        // Translate the email into SendGrid format
        var originalEmailMessage = email.ToEmailMessage();

        // Get the officeName for blob storage pathing. If it's not provided, attempt to look it up based on the officeId.
        var office = await _organizationRepository.GetOfficeByIdAsync(email.OfficeId, email.OrganizationId);
        var officeName = email.OfficeName ?? office?.Name;

        // If there's an attachment, store it in blob storage
        if (email.FileDetails != null)
        {
            var documentPath = await _fileService.SaveDocumentAsync(email.OrganizationId, officeName, email.FileDetails.File,
                email.FileDetails.FileName, email.FileDetails.ContentType, DocumentType.Attachments);

            try
            {
                var createdDocument = await _documentRepository.CreateAsync(new Document
                {
                    OrganizationId = email.OrganizationId,
                    OfficeId = email.OfficeId,
                    PropertyId = email.PropertyId,
                    ReservationId = email.ReservationId,
                    DocumentType = DocumentType.Attachments,
                    FileName = Path.GetFileNameWithoutExtension(email.FileDetails.FileName),
                    FileExtension = Path.GetExtension(email.FileDetails.FileName),
                    ContentType = email.FileDetails.ContentType,
                    DocumentPath = documentPath,
                    CreatedBy = email.CreatedBy
                });

                email.DocumentId = createdDocument.DocumentId;
                email.AttachmentPath = createdDocument.DocumentPath;
                email.AttachmentName = email.FileDetails.FileName;
            }
            catch
            {
                // Best effort cleanup so we don't leave orphaned blobs when metadata create fails.
                await _fileService.DeleteDocumentAsync(email.OrganizationId, email.OfficeName, documentPath);
                throw;
            }
        }

        // Save this email in our database
        var createdEmail = await _emailRepository.CreateEmailAsync(email);
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
            currentEmail = await _emailRepository.UpdateEmailByIdAsync(currentEmail);

            try
            {
                await _emailService.SendEmailAsync(sendGridName, originalEmailMessage);
                currentEmail.EmailStatus = EmailStatus.Succeeded;
                currentEmail.SentOn = DateTimeOffset.UtcNow;
                currentEmail.LastError = "Accepted by SendGrid.";
                currentEmail.ModifiedBy = modifiedBy;
                return await _emailRepository.UpdateEmailByIdAsync(currentEmail);
            }
            catch (Exception ex)
            {
                currentEmail.EmailStatus = EmailStatus.Failed;
                currentEmail.LastError = ex.Message;
                currentEmail.ModifiedBy = modifiedBy;
                currentEmail = await _emailRepository.UpdateEmailByIdAsync(currentEmail);

                if (!IsUnreachable(ex) || attempt >= MaxRetryAttempts)
                    return currentEmail;

                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }

        return currentEmail;
    }

    public async Task AlertTicketListeners(Ticket ticket)
    {
        if (ticket.TicketId == Guid.Empty || ticket.OrganizationId == Guid.Empty)
            return;

        var alerts = await _emailRepository.GetAlertsByTicketIdAsync(ticket.TicketId, ticket.OrganizationId);
        var alertList = alerts?.ToList() ?? [];
        if (alertList.Count == 0)
            return;

        var organization = await _organizationRepository.GetOrganizationByIdAsync(ticket.OrganizationId);
        var sendGridName = organization?.SendGridName;
        var mostRecentNote = GetMostRecentNote(ticket);
        var assignee = string.IsNullOrWhiteSpace(ticket.Assignee) ? "Unassigned" : ticket.Assignee.Trim();
        var ticketState = FormatTicketState(ticket.TicketStateType);
        var ticketCode = string.IsNullOrWhiteSpace(ticket.TicketCode) ? "N/A" : ticket.TicketCode.Trim();
        var ticketTitle = string.IsNullOrWhiteSpace(ticket.Title) ? "N/A" : ticket.Title.Trim();
        var ticketDescription = string.IsNullOrWhiteSpace(ticket.Description) ? "N/A" : ticket.Description.Trim();
        var propertyCode = string.IsNullOrWhiteSpace(ticket.PropertyCode) ? "N/A" : ticket.PropertyCode.Trim();
        var reservationCode = string.IsNullOrWhiteSpace(ticket.ReservationCode) ? "N/A" : ticket.ReservationCode.Trim();
        var actor = ticket.ModifiedBy != Guid.Empty ? ticket.ModifiedBy : ticket.CreatedBy;

        var plainTextContent =
            "Ticket has been updated.\n\n" +
            $"Ticket Code: {ticketCode}\n" +
            $"Title: {ticketTitle}\n" +
            $"Description: {ticketDescription}\n" +
            $"Property: {propertyCode}\n" +
            $"Reservation: {reservationCode}\n" +
            $"Current Assignee: {assignee}\n" +
            $"Current State: {ticketState}\n" +
            $"Most recent Note: {mostRecentNote}";

        var htmlContent =
            "<p>Ticket has been updated.</p>" +
            $"<p><strong>Ticket Code:</strong> {EscapeHtml(ticketCode)}<br>" +
            $"<strong>Title:</strong> {EscapeHtml(ticketTitle)}<br>" +
            $"<strong>Description:</strong> {EscapeHtml(ticketDescription)}<br>" +
            $"<strong>Property:</strong> {EscapeHtml(propertyCode)}<br>" +
            $"<strong>Reservation:</strong> {EscapeHtml(reservationCode)}<br>" +
            $"<strong>Current Assignee:</strong> {EscapeHtml(assignee)}<br>" +
            $"<strong>Current State:</strong> {EscapeHtml(ticketState)}<br>" +
            $"<strong>Most recent Note:</strong> {EscapeHtml(mostRecentNote)}</p>";

        foreach (var alert in alertList)
        {
            if ((alert.ToRecipients?.Count ?? 0) == 0)
                continue;

            var email = new Email
            {
                OrganizationId = ticket.OrganizationId,
                OfficeId = ticket.OfficeId,
                PropertyId = ticket.PropertyId,
                ReservationId = ticket.ReservationId,
                FromRecipient = alert.FromRecipient,
                ToRecipients = alert.ToRecipients,
                CcRecipients = alert.CcRecipients,
                BccRecipients = alert.BccRecipients,
                Subject = $"Ticket Update: {ticketCode}",
                PlainTextContent = plainTextContent,
                HtmlContent = htmlContent,
                EmailType = alert.EmailType,
                EmailStatus = EmailStatus.Attempting,
                CreatedBy = actor
            };

            await SendEmail(sendGridName, email);
        }
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

    private static string GetMostRecentNote(Ticket ticket)
    {
        var latest = (ticket.Notes ?? [])
            .OrderByDescending(note => note.ModifiedOn)
            .ThenByDescending(note => note.CreatedOn)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(latest?.Note) ? "N/A" : latest.Note.Trim();
    }

    private static string FormatTicketState(TicketStateType state)
    {
        return state switch
        {
            TicketStateType.InProgress => "In Progress",
            TicketStateType.WorkComplete => "Work Complete",
            _ => state.ToString()
        };
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }
}
