using System.Text.Json;
using RentAll.Api.Dtos.External;
using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class CreateExternalLeadOwnerDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LocationOfProperty { get; set; }
    public string? ProgramInterest { get; set; }
    public string? WhatIsPromptingContact { get; set; }
    public bool? TimeFrame { get; set; }
    public DateOnly? TargetRentReadyDate { get; set; }
    public string? PropertyGoals { get; set; }
    public string? TellUsMoreAboutYourGoals { get; set; }
    public int? YearsOfExperienceWithRentals { get; set; }
    public string? TellUsMoreAboutProperty { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? NumberOfBeds { get; set; }
    public string? NumberOfBaths { get; set; }
    public string? ApproxSqFootage { get; set; }
    public string? TypeOfProperty { get; set; }
    public string? PropertyCode { get; set; }
    public string? PropertyOffice { get; set; }
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
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
        if (YearsOfExperienceWithRentals.HasValue && YearsOfExperienceWithRentals.Value < 0)
            errors.Add("YearsOfExperienceWithRentals cannot be negative.");
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public static (bool Success, CreateExternalLeadOwnerDto? Dto, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Owner lead data is required");

        var errors = new List<string>();
        ExternalIntakeErrors.CollectRequiredGuid(body, "organizationId", errors, "OrganizationId is required");
        ExternalIntakeErrors.CollectRequiredPositiveInt(body, "officeId", errors, "OfficeId is required.");
        ExternalIntakeErrors.CollectRequiredString(body, "firstName", errors, "FirstName is required");
        ExternalIntakeErrors.CollectRequiredString(body, "lastName", errors, "LastName is required");
        ExternalIntakeErrors.CollectRequiredEmail(body, "email", errors, "Email is required", "Email format is invalid.");
        ExternalIntakeErrors.CollectRequiredString(body, "phone", errors, "Phone is required");
        ExternalIntakeErrors.CollectOptionalNonNegativeInt(body, "yearsOfExperienceWithRentals", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "targetRentReadyDate", errors);
        ExternalIntakeErrors.CollectOptionalStrings(body, ["locationOfProperty", "programInterest", "whatIsPromptingContact", "propertyGoals", "tellUsMoreAboutYourGoals", "tellUsMoreAboutProperty", "address", "city", "state", "zip", "numberOfBeds", "numberOfBaths", "approxSqFootage", "typeOfProperty", "propertyCode", "propertyOffice", "tellUsWhatYouLikeMostAboutYourProperty", "tellUsAnyDrawbacks", "preferredContactMethod", "timeDateForContact", "notes"], errors);
        ExternalIntakeErrors.CollectOptionalBools(body, ["timeFrame", "emailPhoneConsent", "smsConsent"], errors);
        if (errors.Count > 0)
            return (false, null, ExternalIntakeErrors.Join(errors));

        try
        {
            var dto = ExternalIntakeErrors.Deserialize<CreateExternalLeadOwnerDto>(body);
            if (dto == null)
                return (false, null, "Owner lead data is required");

            var (isValid, errorMessage) = dto.IsValid();
            return isValid ? (true, dto, null) : (false, null, errorMessage);
        }
        catch (JsonException ex)
        {
            return (false, null, ExternalIntakeErrors.FromJsonException(ex, body));
        }
    }

    public LeadOwner ToModel(Guid organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = LeadStateType.New,
            AgentId = null,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Phone = Phone,
            LocationOfProperty = LocationOfProperty,
            ProgramInterest = ProgramInterest,
            WhatIsPromptingContact = WhatIsPromptingContact,
            TimeFrame = TimeFrame,
            TargetRentReadyDate = TargetRentReadyDate,
            PropertyGoals = PropertyGoals,
            TellUsMoreAboutYourGoals = TellUsMoreAboutYourGoals,
            YearsOfExperienceWithRentals = YearsOfExperienceWithRentals,
            TellUsMoreAboutProperty = TellUsMoreAboutProperty,
            Address = Address,
            City = City,
            State = State,
            Zip = Zip,
            NumberOfBeds = NumberOfBeds,
            NumberOfBaths = NumberOfBaths,
            ApproxSqFootage = ApproxSqFootage,
            TypeOfProperty = string.Equals(TypeOfProperty?.Trim(), "Condo", StringComparison.OrdinalIgnoreCase) ? PropertyType.Condo : PropertyType.House,
            PropertyCode = PropertyCode,
            PropertyOffice = PropertyOffice,
            TellUsWhatYouLikeMostAboutYourProperty = TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = TellUsAnyDrawbacks,
            PreferredContactMethod = PreferredContactMethod,
            TimeDateForContact = TimeDateForContact,
            Notes = Notes,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = true
        };
}
