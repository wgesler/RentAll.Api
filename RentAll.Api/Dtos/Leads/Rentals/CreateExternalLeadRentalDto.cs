using System.Text.Json;
using RentAll.Api.Dtos.External;
using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Rentals;

public class CreateExternalLeadRentalDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? DesiredLocation { get; set; }
    public string? PropertyRefId { get; set; }
    public string? EstimatedArrivalDate { get; set; }
    public string? EstimatedDepartureDate { get; set; }
    public decimal? MaxMonthlyBudget { get; set; }
    public int? MinBedrooms { get; set; }
    public string? NumberOfOccupants { get; set; }
    public string? WhatBringsYouToTown { get; set; }
    public string? HowDidYouFindUs { get; set; }
    public string? TellUsMoreAboutHowYouFoundUs { get; set; }
    public bool? PetFriendly { get; set; }
    public DateOnly? DecisionDate { get; set; }
    public string? OrganizationName { get; set; }
    public string? AdditionalInformation { get; set; }
    public string? Notes { get; set; }
    public bool INeedAsap { get; set; }
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
        if (MaxMonthlyBudget.HasValue && MaxMonthlyBudget.Value < 0)
            errors.Add("MaxMonthlyBudget cannot be negative.");
        if (MinBedrooms.HasValue && MinBedrooms.Value < 0)
            errors.Add("MinBedrooms cannot be negative.");
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public static (bool Success, CreateExternalLeadRentalDto? Dto, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Rental lead data is required");

        var errors = new List<string>();
        ExternalIntakeErrors.CollectRequiredGuid(body, "organizationId", errors, "OrganizationId is required");
        ExternalIntakeErrors.CollectRequiredPositiveInt(body, "officeId", errors, "OfficeId is required.");
        ExternalIntakeErrors.CollectRequiredString(body, "firstName", errors, "FirstName is required");
        ExternalIntakeErrors.CollectRequiredString(body, "lastName", errors, "LastName is required");
        ExternalIntakeErrors.CollectRequiredEmail(body, "email", errors, "Email is required", "Email format is invalid.");
        ExternalIntakeErrors.CollectRequiredString(body, "phone", errors, "Phone is required");
        ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(body, "maxMonthlyBudget", errors);
        ExternalIntakeErrors.CollectOptionalNonNegativeInt(body, "minBedrooms", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "decisionDate", errors);
        ExternalIntakeErrors.CollectOptionalStrings(body, ["desiredLocation", "propertyRefId", "estimatedArrivalDate", "estimatedDepartureDate", "numberOfOccupants", "whatBringsYouToTown", "howDidYouFindUs", "tellUsMoreAboutHowYouFoundUs", "organizationName", "additionalInformation", "notes"], errors);
        ExternalIntakeErrors.CollectOptionalBools(body, ["petFriendly", "iNeedAsap", "emailPhoneConsent", "smsConsent"], errors);
        if (errors.Count > 0)
            return (false, null, ExternalIntakeErrors.Join(errors));

        try
        {
            var dto = ExternalIntakeErrors.Deserialize<CreateExternalLeadRentalDto>(body);
            if (dto == null)
                return (false, null, "Rental lead data is required");

            var (isValid, errorMessage) = dto.IsValid();
            return isValid ? (true, dto, null) : (false, null, errorMessage);
        }
        catch (JsonException ex)
        {
            return (false, null, ExternalIntakeErrors.FromJsonException(ex, body));
        }
    }

    public LeadRental ToModel(Guid organizationId) =>
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
            DesiredLocation = DesiredLocation,
            PropertyRefId = PropertyRefId,
            EstimatedArrivalDate = EstimatedArrivalDate,
            EstimatedDepartureDate = EstimatedDepartureDate,
            MaxMonthlyBudget = MaxMonthlyBudget,
            MinBedrooms = MinBedrooms,
            NumberOfOccupants = NumberOfOccupants,
            WhatBringsYouToTown = WhatBringsYouToTown,
            HowDidYouFindUs = HowDidYouFindUs,
            TellUsMoreAboutHowYouFoundUs = TellUsMoreAboutHowYouFoundUs,
            PetFriendly = PetFriendly,
            DecisionDate = DecisionDate,
            OrganizationName = OrganizationName,
            AdditionalInformation = AdditionalInformation,
            Notes = Notes,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            INeedAsap = INeedAsap,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = true
        };
}
