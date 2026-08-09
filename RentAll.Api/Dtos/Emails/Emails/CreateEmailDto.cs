using RentAll.Domain.Models.Common;
using System.Text.RegularExpressions;

namespace RentAll.Api.Dtos.Emails.Emails;

public class CreateEmailDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string? OfficeName { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public EmailAddress FromRecipient { get; set; } = new();
    public List<EmailAddress> ToRecipients { get; set; } = [];
    public List<EmailAddress> CcRecipients { get; set; } = [];
    public List<EmailAddress> BccRecipients { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public int EmailTypeId { get; set; }
    public FileDetails? FileDetails { get; set; }
    public List<FileDetails> AdditionalFileDetails { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organization, string officeAccess)
    {
        ToRecipients ??= [];
        CcRecipients ??= [];
        BccRecipients ??= [];
        AdditionalFileDetails ??= [];
        FromRecipient ??= new EmailAddress();

        if (OrganizationId == Guid.Empty || OrganizationId != organization)
            return (false, "OrganizationId not valid");

        var officeIds = (officeAccess ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var officeId) ? officeId : -1)
            .Where(officeId => officeId > 0)
            .ToHashSet();
        if (!officeIds.Contains(OfficeId))
            return (false, "OfficeId not valid");

        if (ToRecipients.Count == 0)
            return (false, "At least one ToRecipient is required");

        if (string.IsNullOrWhiteSpace(FromRecipient.Email))
            return (false, "FromRecipient.Email is required");

        if (!ToRecipients.All(recipient => IsValidEmail(recipient.Email)))
            return (false, "One or more ToRecipients have invalid email addresses");

        if (!IsValidEmail(FromRecipient.Email))
            return (false, "FromRecipient.Email is not a valid email address");

        if (!CcRecipients.All(recipient => string.IsNullOrWhiteSpace(recipient.Email) || IsValidEmail(recipient.Email)))
            return (false, "One or more CcRecipients have invalid email addresses");

        if (!BccRecipients.All(recipient => string.IsNullOrWhiteSpace(recipient.Email) || IsValidEmail(recipient.Email)))
            return (false, "One or more BccRecipients have invalid email addresses");

        if (string.IsNullOrWhiteSpace(Subject))
            return (false, "Subject is required");

        if (string.IsNullOrWhiteSpace(PlainTextContent) && string.IsNullOrWhiteSpace(HtmlContent))
            return (false, "Either PlainTextContent or HtmlContent is required");

        if (!Enum.IsDefined(typeof(EmailType), EmailTypeId))
            return (false, $"Invalid EmailType value: {EmailTypeId}");

        if (AdditionalFileDetails.Count > 5)
            return (false, "A maximum of 5 additional attachments is allowed");

        foreach (var additionalFile in AdditionalFileDetails)
        {
            if (additionalFile == null || string.IsNullOrWhiteSpace(additionalFile.FileName))
                return (false, "Each additional attachment requires a file name");

            if (string.IsNullOrWhiteSpace(additionalFile.File) && string.IsNullOrWhiteSpace(additionalFile.DataUrl))
                return (false, $"Additional attachment '{additionalFile.FileName}' is missing file content");
        }

        return (true, null);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        try
        {
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public Email ToModel(Guid currentUser)
    {
        return new Email
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            FromRecipient = EmailAddress.Create(FromRecipient.Email, FromRecipient.Name),
            ToRecipients = ToRecipients
                .Select(recipient => EmailAddress.Create(recipient.Email, recipient.Name))
                .ToList(),
            CcRecipients = CcRecipients
                .Select(recipient => EmailAddress.Create(recipient.Email, recipient.Name))
                .ToList(),
            BccRecipients = BccRecipients
                .Select(recipient => EmailAddress.Create(recipient.Email, recipient.Name))
                .ToList(),
            Subject = Subject,
            PlainTextContent = PlainTextContent,
            HtmlContent = HtmlContent,
            EmailType = (EmailType)EmailTypeId,
            FileDetails = FileDetails,
            AdditionalFileDetails = AdditionalFileDetails ?? [],
            EmailStatus = EmailStatus.Attempting,
            CreatedBy = currentUser
        };
    }
}
