using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Partners;

public class UpdateLeadPartnerDto
{
    public int PartnerId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Title { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? MarketsCitiesServed { get; set; }
    public string? FurnishedPropertiesInPortfolio { get; set; }
    public string? AboutYourBusiness { get; set; }
    public string? Notes { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (PartnerId <= 0)
            return (false, "PartnerId is required.");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (!LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (!string.IsNullOrWhiteSpace(currentOffices)
            && !currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        return (true, null);
    }

    public LeadPartner ToModel(Guid organizationId, Guid currentUser) =>
        new()
        {
            PartnerId = PartnerId,
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = (LeadStateType)LeadStateId,
            Name = Name,
            CompanyName = CompanyName,
            Title = Title,
            Email = Email,
            Phone = Phone,
            MarketsCitiesServed = MarketsCitiesServed,
            FurnishedPropertiesInPortfolio = FurnishedPropertiesInPortfolio,
            AboutYourBusiness = AboutYourBusiness,
            Notes = Notes,
            ModifiedBy = currentUser,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = IsActive
        };
}
