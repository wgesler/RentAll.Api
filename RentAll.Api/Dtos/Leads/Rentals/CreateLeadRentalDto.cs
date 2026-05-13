using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Rentals;

public class CreateLeadRentalDto
{
    public Guid OrganizationId { get; set; }
    public int LeadStateId { get; set; }
    public int OfficeId { get; set; }
    public Guid? AgentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
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
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (currentOffices == null && OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

        if (AgentId.HasValue && AgentId.Value == Guid.Empty)
            return (false, "AgentId cannot be an empty GUID when supplied.");

        if (!string.IsNullOrWhiteSpace(Email) && !LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (MaxMonthlyBudget.HasValue && MaxMonthlyBudget.Value < 0)
            return (false, "MaxMonthlyBudget cannot be negative.");

        if (MinBedrooms.HasValue && MinBedrooms.Value < 0)
            return (false, "MinBedrooms cannot be negative.");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (!string.IsNullOrWhiteSpace(currentOffices)
            && !currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        return (true, null);
    }

    public LeadRental ToModel(Guid organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            LeadState = (LeadStateType)LeadStateId,
            AgentId = AgentId,
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
            IsActive = IsActive
        };

}
