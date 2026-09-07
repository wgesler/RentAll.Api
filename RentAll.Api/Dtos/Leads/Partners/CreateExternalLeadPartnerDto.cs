using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Partners;

public class CreateExternalLeadPartnerDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (!LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        return (true, null);
    }

    public LeadPartner ToModel(Guid organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = LeadStateType.New,
            Name = Name,
            CompanyName = CompanyName,
            Title = Title,
            Email = Email,
            Phone = Phone,
            MarketsCitiesServed = MarketsCitiesServed,
            FurnishedPropertiesInPortfolio = FurnishedPropertiesInPortfolio,
            AboutYourBusiness = AboutYourBusiness,
            Notes = Notes,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = true
        };
}
