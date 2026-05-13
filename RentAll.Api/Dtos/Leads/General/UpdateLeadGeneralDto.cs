using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.General;

public class UpdateLeadGeneralDto
{
    public int GeneralId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Location { get; set; }
    public string? Email { get; set; }
    public string? PhoneMobile { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (GeneralId <= 0)
            return (false, "GeneralId is required.");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

        if (!string.IsNullOrWhiteSpace(Email) && !LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

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
            GeneralId = GeneralId,
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
