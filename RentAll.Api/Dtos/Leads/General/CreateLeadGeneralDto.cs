using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.General;

public class CreateLeadGeneralDto
{
    public Guid OrganizationId { get; set; }
    public int LeadStateId { get; set; }
    public int OfficeId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (currentOffices == null && OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, "FirstName is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, "LastName is required");

        if (string.IsNullOrWhiteSpace(Location))
            return (false, "Location is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (!LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (string.IsNullOrWhiteSpace(PhoneMobile))
            return (false, "PhoneMobile is required");

        if (string.IsNullOrWhiteSpace(Message))
            return (false, "Message is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (!string.IsNullOrWhiteSpace(currentOffices)
            && !currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        return (true, null);
    }

    public LeadGeneral ToModel(Guid organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = (LeadStateType)LeadStateId,
            FirstName = FirstName?.Trim(),
            LastName = LastName?.Trim(),
            Location = Location?.Trim(),
            Email = Email?.Trim(),
            PhoneMobile = PhoneMobile?.Trim(),
            Message = Message?.Trim(),
            IsActive = IsActive
        };
}
