using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class UpdateLeadOwnerDto
{
    public int OwnerId { get; set; }
    public int OfficeId { get; set; }
    public int LeadStateId { get; set; }
    public Guid? AgentId { get; set; }
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
    public bool PurchaseKitchenItems { get; set; }
    public decimal? KitchenBudget { get; set; }
    public bool FurnishUnit { get; set; }
    public decimal? FurnishBudget { get; set; }
    public string? NumberOfBeds { get; set; }
    public string? NumberOfBaths { get; set; }
    public string? ApproxSqFootage { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PropertyCode { get; set; }
    public string? PropertyOffice { get; set; }
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
    public string? Notes { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (OwnerId <= 0)
            return (false, "OwnerId is required.");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (!string.IsNullOrWhiteSpace(Email) && !LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (!string.IsNullOrWhiteSpace(currentOffices)
            && !currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (PropertyTypeId.HasValue && !Enum.IsDefined(typeof(PropertyType), PropertyTypeId.Value))
            return (false, $"Invalid PropertyTypeId value: {PropertyTypeId.Value}");

        return (true, null);
    }

    public LeadOwner ToModel(Guid currentUser) =>
        new()
        {
            OwnerId = OwnerId,
            OfficeId = OfficeId,
            LeadState = (LeadStateType)LeadStateId,
            AgentId = AgentId,
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
            PurchaseKitchenItems = PurchaseKitchenItems,
            KitchenBudget = KitchenBudget,
            FurnishUnit = FurnishUnit,
            FurnishBudget = FurnishBudget,
            NumberOfBeds = NumberOfBeds,
            NumberOfBaths = NumberOfBaths,
            ApproxSqFootage = ApproxSqFootage,
            TypeOfProperty = PropertyTypeId.HasValue ? (PropertyType?)PropertyTypeId.Value : null,
            PropertyCode = PropertyCode,
            PropertyOffice = PropertyOffice,
            TellUsWhatYouLikeMostAboutYourProperty = TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = TellUsAnyDrawbacks,
            PreferredContactMethod = PreferredContactMethod,
            TimeDateForContact = TimeDateForContact,
            Notes = Notes,
            ModifiedBy = currentUser,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = IsActive
        };
}
