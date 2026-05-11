using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class LeadOwnerResponseDto
{
    public int OwnerId { get; set; }
    public int LeadStateId { get; set; }
    public Guid? AgentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
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
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
    public bool? EmailPhoneConsent { get; set; }
    public bool? SmsConsent { get; set; }

    public LeadOwnerResponseDto(LeadOwner owner)
    {
        OwnerId = owner.OwnerId;
        LeadStateId = (int)owner.LeadState;
        AgentId = owner.AgentId;
        FirstName = owner.FirstName;
        LastName = owner.LastName;
        Email = owner.Email;
        Phone = owner.Phone;
        LocationOfProperty = owner.LocationOfProperty;
        ProgramInterest = owner.ProgramInterest;
        WhatIsPromptingContact = owner.WhatIsPromptingContact;
        TimeFrame = owner.TimeFrame;
        TargetRentReadyDate = owner.TargetRentReadyDate;
        PropertyGoals = owner.PropertyGoals;
        TellUsMoreAboutYourGoals = owner.TellUsMoreAboutYourGoals;
        YearsOfExperienceWithRentals = owner.YearsOfExperienceWithRentals;
        TellUsMoreAboutProperty = owner.TellUsMoreAboutProperty;
        Address = owner.Address;
        City = owner.City;
        State = owner.State;
        Zip = owner.Zip;
        NumberOfBeds = owner.NumberOfBeds;
        NumberOfBaths = owner.NumberOfBaths;
        ApproxSqFootage = owner.ApproxSqFootage;
        TypeOfProperty = owner.TypeOfProperty;
        TellUsWhatYouLikeMostAboutYourProperty = owner.TellUsWhatYouLikeMostAboutYourProperty;
        TellUsAnyDrawbacks = owner.TellUsAnyDrawbacks;
        PreferredContactMethod = owner.PreferredContactMethod;
        TimeDateForContact = owner.TimeDateForContact;
        EmailPhoneConsent = owner.EmailPhoneConsent;
        SmsConsent = owner.SmsConsent;
    }
}
