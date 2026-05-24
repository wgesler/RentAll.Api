using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Leads;

public class LeadOwner
{
    public int OwnerId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public LeadStateType LeadState { get; set; }
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
    public bool PurchaseKitchenItems { get; set; }
    public decimal? KitchenBudget { get; set; }
    public bool FurnishUnit { get; set; }
    public decimal? FurnishBudget { get; set; }
    public string? NumberOfBeds { get; set; }
    public string? NumberOfBaths { get; set; }
    public string? ApproxSqFootage { get; set; }
    public PropertyType? TypeOfProperty { get; set; }
    public string? PropertyCode { get; set; }
    public string? PropertyOffice { get; set; }
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; } = true;
}
