using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Emails.Alerts;

public class AlertResponseDto
{
    public Guid AlertId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public List<EmailAddress> ToRecipients { get; set; } = [];
    public List<EmailAddress> CcRecipients { get; set; } = [];
    public List<EmailAddress> BccRecipients { get; set; } = [];
    public EmailAddress FromRecipient { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public int EmailTypeId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? NextAlertDate { get; set; }
    public int? DaysBeforeDeparture { get; set; }
    public int FrequencyId { get; set; }
    public int EmailStatusId { get; set; }
    public int AttemptCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTimeOffset? LastAttemptedOn { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public AlertResponseDto(Alert alert)
    {
        AlertId = alert.AlertId;
        OrganizationId = alert.OrganizationId;
        OfficeId = alert.OfficeId;
        PropertyId = alert.PropertyId;
        PropertyCode = alert.PropertyCode;
        ReservationId = alert.ReservationId;
        ReservationCode = alert.ReservationCode;
        ToRecipients = alert.ToRecipients;
        CcRecipients = alert.CcRecipients;
        BccRecipients = alert.BccRecipients;
        FromRecipient = alert.FromRecipient;
        Subject = alert.Subject;
        PlainTextContent = alert.PlainTextContent;
        EmailTypeId = (int)alert.EmailType;
        StartDate = alert.StartDate;
        NextAlertDate = alert.NextAlertDate;
        DaysBeforeDeparture = alert.DaysBeforeDeparture;
        FrequencyId = (int)alert.Frequency;
        EmailStatusId = (int)alert.EmailStatus;
        AttemptCount = alert.AttemptCount;
        LastError = alert.LastError;
        LastAttemptedOn = alert.LastAttemptedOn;
        SentOn = alert.SentOn;
        IsActive = alert.IsActive;
        CreatedOn = alert.CreatedOn;
        CreatedBy = alert.CreatedBy;
        ModifiedOn = alert.ModifiedOn;
        ModifiedBy = alert.ModifiedBy;
    }
}
