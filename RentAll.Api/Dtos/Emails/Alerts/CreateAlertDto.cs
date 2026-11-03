using RentAll.Domain.Models.Common;
using System.Text.RegularExpressions;

namespace RentAll.Api.Dtos.Emails.Alerts;

public class CreateAlertDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? TicketId { get; set; }
    public EmailAddress FromRecipient { get; set; } = new();
    public List<EmailAddress> ToRecipients { get; set; } = [];
    public List<EmailAddress> CcRecipients { get; set; } = [];
    public List<EmailAddress> BccRecipients { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public int EmailTypeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? DaysBeforeDeparture { get; set; }
    public bool MatchDepartureDate { get; set; }
    public int FrequencyId { get; set; }
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organization, string officeAccess)
    {
        ToRecipients ??= [];
        CcRecipients ??= [];
        BccRecipients ??= [];
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

        if (ToRecipients.Count == 0 || !ToRecipients.All(recipient => IsValidEmail(recipient.Email)))
            return (false, "A valid To address is required");

        if (string.IsNullOrWhiteSpace(Subject))
            return (false, "Subject is required");

        if (!StartDate.HasValue)
            return (false, "StartDate is required");

        if (!Enum.IsDefined(typeof(FrequencyType), FrequencyId) || FrequencyId == (int)FrequencyType.NA)
            return (false, "Frequency is required");

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

    public Alert ToModel(Guid currentUser)
    {
        return new Alert
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            TicketId = TicketId,
            FromRecipient = EmailAddress.Create(FromRecipient.Email ?? string.Empty, FromRecipient.Name),
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
            PlainTextContent = PlainTextContent ?? string.Empty,
            EmailType = (EmailType)EmailTypeId,
            StartDate = StartDate,
            EndDate = EndDate,
            DaysBeforeDeparture = DaysBeforeDeparture,
            MatchDepartureDate = MatchDepartureDate,
            Frequency = (FrequencyType)FrequencyId,
            EmailStatus = EmailStatus.Unsent,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}
