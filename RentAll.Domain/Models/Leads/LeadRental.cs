using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Leads;

public class LeadRental
{
    public int RentalId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public LeadStateType LeadState { get; set; }
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
    public bool IsActive { get; set; } = true;
}
