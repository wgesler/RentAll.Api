using RentAll.Domain.Enums;
using RentAll.Domain.Models.Common;

namespace RentAll.Domain.Models;

public class Alert
{
    public Guid AlertId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public DateTimeOffset? ArrivalDate { get; set; }
    public DateTimeOffset? DepartureDate { get; set; }
    public List<EmailAddress> ToRecipients { get; set; } = [];
    public List<EmailAddress> CcRecipients { get; set; } = [];
    public List<EmailAddress> BccRecipients { get; set; } = [];
    public EmailAddress FromRecipient { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string PlainTextContent { get; set; } = string.Empty;
    public EmailType EmailType { get; set; } = EmailType.Other;
    public DateTimeOffset? StartDate { get; set; }
    public int? DaysBeforeDeparture { get; set; }
    public DateTimeOffset? NextAlertDate { get; set; }
    public FrequencyType Frequency { get; set; }
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Unsent;
    public int AttemptCount { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTimeOffset? LastAttemptedOn { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
