using System.Net;
using System.Text.Json;
using RentAll.Api.Dtos.External;

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
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");
        if (OfficeId <= 0)
            errors.Add("OfficeId is required");
        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("FirstName is required");
        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add("LastName is required");
        if (string.IsNullOrWhiteSpace(Location))
            errors.Add("Location is required");
        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        if (string.IsNullOrWhiteSpace(Phone))
            errors.Add("Phone is required");
        if (string.IsNullOrWhiteSpace(Address))
            errors.Add("Address is required");
        if (HasPermissionToEnter == null)
            errors.Add("HasPermissionToEnter is required");
        if (string.IsNullOrWhiteSpace(IssueDescription))
            errors.Add("IssueDescription is required");
        if (CommunicationConsent == null)
            errors.Add("CommunicationConsent is required");
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public static (bool Success, CreateExternalTicketDto? Dto, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Ticket data is required");

        var errors = new List<string>();
        ExternalIntakeErrors.CollectRequiredGuid(body, "organizationId", errors, "OrganizationId is required");
        ExternalIntakeErrors.CollectRequiredPositiveInt(body, "officeId", errors, "OfficeId is required");
        ExternalIntakeErrors.CollectRequiredString(body, "firstName", errors, "FirstName is required");
        ExternalIntakeErrors.CollectRequiredString(body, "lastName", errors, "LastName is required");
        ExternalIntakeErrors.CollectRequiredString(body, "location", errors, "Location is required");
        ExternalIntakeErrors.CollectRequiredString(body, "email", errors, "Email is required");
        ExternalIntakeErrors.CollectRequiredString(body, "phone", errors, "Phone is required");
        ExternalIntakeErrors.CollectRequiredString(body, "address", errors, "Address is required");
        ExternalIntakeErrors.CollectRequiredBool(body, "hasPermissionToEnter", errors, "HasPermissionToEnter is required");
        ExternalIntakeErrors.CollectRequiredString(body, "issueDescription", errors, "IssueDescription is required");
        ExternalIntakeErrors.CollectRequiredBool(body, "communicationConsent", errors, "CommunicationConsent is required");
        ExternalIntakeErrors.CollectOptionalBool(body, "smsConsent", errors);
        if (errors.Count > 0)
            return (false, null, ExternalIntakeErrors.Join(errors));

        try
        {
            var dto = ExternalIntakeErrors.Deserialize<CreateExternalTicketDto>(body);
            if (dto == null)
                return (false, null, "Ticket data is required");

            var (isValid, errorMessage) = dto.IsValid();
            return isValid ? (true, dto, null) : (false, null, errorMessage);
        }
        catch (JsonException ex)
        {
            return (false, null, ExternalIntakeErrors.FromJsonException(ex, body));
        }
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
            StepsToReproduce = null,
            TicketStateType = TicketStateType.Created,
            NeedPermissionToEnter = true,
            PermissionGranted = HasPermissionToEnter ?? false,
            OwnerContacted = false,
            ConfirmedWithTenant = false,
            FollowedUpWithOwner = false,
            WorkOrderCompleted = false,
            IsForRentAll = false,
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
        AddLine(metaLines, "Phone", Phone);
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
