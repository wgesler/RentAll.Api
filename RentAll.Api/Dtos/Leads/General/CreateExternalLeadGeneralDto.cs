using RentAll.Api.Dtos.External;
using RentAll.Domain.Models.Leads;
using System.Text.Json;

namespace RentAll.Api.Dtos.Leads.General;

public class CreateExternalLeadGeneralDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");
        if (OfficeId <= 0)
            errors.Add("OfficeId is required.");
        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add("FirstName is required");
        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add("LastName is required");
        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!LeadDtoValidation.IsValidEmail(Email))
            errors.Add("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(Phone))
            errors.Add("Phone is required");
        if (string.IsNullOrWhiteSpace(Message))
            errors.Add("Message is required");
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public static (bool Success, CreateExternalLeadGeneralDto? Dto, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "General lead data is required");

        var errors = new List<string>();
        ExternalIntakeErrors.CollectRequiredGuid(body, "organizationId", errors, "OrganizationId is required");
        ExternalIntakeErrors.CollectRequiredPositiveInt(body, "officeId", errors, "OfficeId is required.");
        ExternalIntakeErrors.CollectRequiredString(body, "firstName", errors, "FirstName is required");
        ExternalIntakeErrors.CollectRequiredString(body, "lastName", errors, "LastName is required");
        ExternalIntakeErrors.CollectRequiredEmail(body, "email", errors, "Email is required", "Email format is invalid.");
        ExternalIntakeErrors.CollectRequiredString(body, "phone", errors, "Phone is required");
        ExternalIntakeErrors.CollectRequiredString(body, "message", errors, "Message is required");
        ExternalIntakeErrors.CollectOptionalString(body, "notes", errors);
        if (errors.Count > 0)
            return (false, null, ExternalIntakeErrors.Join(errors));

        try
        {
            var dto = ExternalIntakeErrors.Deserialize<CreateExternalLeadGeneralDto>(body);
            if (dto == null)
                return (false, null, "General lead data is required");

            var (isValid, errorMessage) = dto.IsValid();
            return isValid ? (true, dto, null) : (false, null, errorMessage);
        }
        catch (JsonException ex)
        {
            return (false, null, ExternalIntakeErrors.FromJsonException(ex, body));
        }
    }

    public LeadGeneral ToModel(Guid organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = LeadStateType.New,
            FirstName = FirstName?.Trim(),
            LastName = LastName?.Trim(),
            Email = Email?.Trim(),
            PhoneMobile = Phone?.Trim(),
            Message = Message?.Trim(),
            Notes = Notes?.Trim(),
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            IsActive = true
        };
}
