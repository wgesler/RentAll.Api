using System.Net;

namespace RentAll.Api.Dtos.Tickets.Tickets;

public class CreateExternalTicketDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
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

        if (string.IsNullOrWhiteSpace(Phone))
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
        var fullName = string.Join(" ", new[] { FirstName?.Trim(), LastName?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
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
            Title = $"Maintenance Request: From {fullName}",
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
        var fullName = string.Join(" ", new[] { FirstName?.Trim(), LastName?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var issueLines = new List<string>();
        AddBlock(issueLines, "Issue Description", IssueDescription);
        var issueHtml = string.Join("<br />", issueLines);

        var metaLines = new List<string>();
        AddLine(metaLines, "Name", fullName);
        AddLine(metaLines, "Location", Location);
        AddLine(metaLines, "Email", Email);
        AddLine(metaLines, "Phone/Mobile", PhoneMobile);
        AddLine(metaLines, "Address", Address);
        AddLine(metaLines, "Permission To Enter", ToYesNo(HasPermissionToEnter));
        AddLine(metaLines, "Communication Consent", ToYesNo(CommunicationConsent));
        AddLine(metaLines, "SMS Consent", ToYesNo(SmsConsent));

        var metaHtml = string.Join("<br />", metaLines);
        if (string.IsNullOrWhiteSpace(issueHtml))
        {
            return metaHtml;
        }

        if (string.IsNullOrWhiteSpace(metaHtml))
        {
            return issueHtml;
        }

        return $"{issueHtml}<br /><br />{metaHtml}";
    }

    private static void AddLine(List<string> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        lines.Add($"<strong>{WebUtility.HtmlEncode(label)}:</strong> {WebUtility.HtmlEncode(value.Trim())}");
    }

    private static void AddBlock(List<string> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        lines.Add($"<strong>{WebUtility.HtmlEncode(label)}:</strong>");
        var encoded = WebUtility.HtmlEncode(value.Trim());
        lines.Add(encoded.Replace("\r\n", "\n").Replace("\n", "<br />"));
    }

    private static string? ToYesNo(bool? value)
    {
        if (value == null)
            return null;

        return value.Value ? "Yes" : "No";
    }
}
