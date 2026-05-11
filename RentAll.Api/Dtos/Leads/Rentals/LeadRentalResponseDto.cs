using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Rentals;

public class LeadRentalResponseDto
{
    public int RentalId { get; set; }
    public int LeadStateId { get; set; }
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

    public LeadRentalResponseDto(LeadRental rental)
    {
        RentalId = rental.RentalId;
        LeadStateId = (int)rental.LeadState;
        AgentId = rental.AgentId;
        FirstName = rental.FirstName;
        LastName = rental.LastName;
        Email = rental.Email;
        Phone = rental.Phone;
        DesiredLocation = rental.DesiredLocation;
        PropertyRefId = rental.PropertyRefId;
        EstimatedArrivalDate = rental.EstimatedArrivalDate;
        EstimatedDepartureDate = rental.EstimatedDepartureDate;
        MaxMonthlyBudget = rental.MaxMonthlyBudget;
        MinBedrooms = rental.MinBedrooms;
        NumberOfOccupants = rental.NumberOfOccupants;
        WhatBringsYouToTown = rental.WhatBringsYouToTown;
        HowDidYouFindUs = rental.HowDidYouFindUs;
        TellUsMoreAboutHowYouFoundUs = rental.TellUsMoreAboutHowYouFoundUs;
        PetFriendly = rental.PetFriendly;
        DecisionDate = rental.DecisionDate;
        OrganizationName = rental.OrganizationName;
        AdditionalInformation = rental.AdditionalInformation;
        INeedAsap = rental.INeedAsap;
        EmailPhoneConsent = rental.EmailPhoneConsent;
        SmsConsent = rental.SmsConsent;
        IsActive = rental.IsActive;
    }
}
