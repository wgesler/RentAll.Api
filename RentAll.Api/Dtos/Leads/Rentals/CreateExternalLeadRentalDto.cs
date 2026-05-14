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
    public bool INeedAsap { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }

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

        if (MaxMonthlyBudget.HasValue && MaxMonthlyBudget.Value < 0)
            return (false, "MaxMonthlyBudget cannot be negative.");

        if (MinBedrooms.HasValue && MinBedrooms.Value < 0)
            return (false, "MinBedrooms cannot be negative.");

        return (true, null);
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
            INeedAsap = INeedAsap,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = true
        };
}
