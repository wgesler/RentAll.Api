using RentAll.Domain.Models.Leads;

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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "FirstName is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "LastName is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (!LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (string.IsNullOrWhiteSpace(Message))
            return (false, "Message is required");

        return (true, null);
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
            IsActive = true
        };
}
