using System.Text.Json;
using RentAll.Api.Dtos.External;
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
    public string? HowHearAboutUs { get; set; }
    public string? Notes { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");
        if (OfficeId <= 0)
            errors.Add("OfficeId is required.");
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required");
        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!LeadDtoValidation.IsValidEmail(Email))
            errors.Add("Email format is invalid.");
        if (string.IsNullOrWhiteSpace(Phone))
            errors.Add("Phone is required");
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public static (bool Success, CreateExternalLeadPartnerDto? Dto, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Partner lead data is required");

        var errors = new List<string>();
        ExternalIntakeErrors.CollectRequiredGuid(body, "organizationId", errors, "OrganizationId is required");
        ExternalIntakeErrors.CollectRequiredPositiveInt(body, "officeId", errors, "OfficeId is required.");
        ExternalIntakeErrors.CollectRequiredString(body, "name", errors, "Name is required");
        ExternalIntakeErrors.CollectRequiredEmail(body, "email", errors, "Email is required", "Email format is invalid.");
        ExternalIntakeErrors.CollectRequiredString(body, "phone", errors, "Phone is required");
        ExternalIntakeErrors.CollectOptionalStrings(body, ["companyName", "title", "marketsCitiesServed", "furnishedPropertiesInPortfolio", "aboutYourBusiness", "howHearAboutUs", "notes"], errors);
        ExternalIntakeErrors.CollectOptionalBools(body, ["emailPhoneConsent", "smsConsent"], errors);
        if (errors.Count > 0)
            return (false, null, ExternalIntakeErrors.Join(errors));

        try
        {
            var dto = ExternalIntakeErrors.Deserialize<CreateExternalLeadPartnerDto>(body);
            if (dto == null)
                return (false, null, "Partner lead data is required");

            var (isValid, errorMessage) = dto.IsValid();
            return isValid ? (true, dto, null) : (false, null, errorMessage);
        }
        catch (JsonException ex)
        {
            return (false, null, ExternalIntakeErrors.FromJsonException(ex, body));
        }
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
            HowHearAboutUs = HowHearAboutUs,
            Notes = Notes,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = true
        };
}
