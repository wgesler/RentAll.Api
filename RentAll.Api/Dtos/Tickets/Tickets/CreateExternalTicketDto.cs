using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class CreateExternalTicketDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Address { get; set; }
    public bool? HasPermissionToEnter { get; set; }
    public string? IssueDescription { get; set; }
    public bool? CommunicationConsent { get; set; }
    public bool? SmsConsent { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "FirstName is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "LastName is required");

        if (string.IsNullOrWhiteSpace(Location))
            return (false, "Location is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (string.IsNullOrWhiteSpace(PhoneMobile))
            return (false, "PhoneMobile is required");

        if (string.IsNullOrWhiteSpace(Address))
            return (false, "Address is required");

        if (HasPermissionToEnter == null)
            return (false, "HasPermissionToEnter is required");

        if (string.IsNullOrWhiteSpace(IssueDescription))
            return (false, "IssueDescription is required");

        if (CommunicationConsent == null)
            return (false, "CommunicationConsent is required");

        return (true, null);
    }

    public Ticket ToModel(string code, Guid createdBy)
    {
        return new Ticket
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = null,
            ReservationId = null,
            ReservationCode = null,
            AssigneeId = null,
            AgentId = null,
            TicketCode = code,
            Title = "Maintenance Request",
            Description = BuildDigestDescription(),
            TicketStateType = TicketStateType.Created,
            NeedPermissionToEnter = true,
            PermissionGranted = HasPermissionToEnter ?? false,
            OwnerContacted = false,
            ConfirmedWithTenant = false,
            FollowedUpWithOwner = false,
            WorkOrderCompleted = false,
            Notes = new List<TicketNote>(),
            IsActive = true,
            CreatedBy = createdBy
        };
    }

    private string BuildDigestDescription()
    {
        var lines = new List<string>();
        var fullName = string.Join(" ", new[] { FirstName?.Trim(), LastName?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
        AddLine(lines, "Name", fullName);
        AddLine(lines, "Location", Location);
        AddLine(lines, "Email", Email);
        AddLine(lines, "Phone/Mobile", PhoneMobile);
        AddLine(lines, "Address", Address);
        AddLine(lines, "Permission To Enter", ToYesNo(HasPermissionToEnter));
        AddLine(lines, "Communication Consent", ToYesNo(CommunicationConsent));
        AddLine(lines, "SMS Consent", ToYesNo(SmsConsent));

        AddBlock(lines, "Issue Description", IssueDescription);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddLine(List<string> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        lines.Add($"**{label}:** {value.Trim()}");
    }

    private static void AddBlock(List<string> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        lines.Add($"**{label}:**");
        lines.Add(value.Trim());
    }

    private static string? ToYesNo(bool? value)
    {
        if (value == null)
            return null;

        return value.Value ? "Yes" : "No";
    }
}
