using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class CreateLeadOwnerDto
{
    public Guid OrganizationId { get; set; }
    public int LeadStateId { get; set; }
    public int OfficeId { get; set; }
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
    public string? NumberOfBeds { get; set; }
    public string? NumberOfBaths { get; set; }
    public string? ApproxSqFootage { get; set; }
    public string? TypeOfProperty { get; set; }
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string? currentOffices)
    {
        if (currentOffices == null && OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (!Enum.IsDefined(typeof(LeadStateType), LeadStateId))
            return (false, $"Invalid LeadStateId value: {LeadStateId}");

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

        if (AgentId.HasValue && AgentId.Value == Guid.Empty)
            return (false, "AgentId cannot be an empty GUID when supplied.");

        if (!string.IsNullOrWhiteSpace(Email) && !LeadDtoValidation.IsValidEmail(Email))
            return (false, "Email format is invalid.");

        if (YearsOfExperienceWithRentals.HasValue && YearsOfExperienceWithRentals.Value < 0)
            return (false, "YearsOfExperienceWithRentals cannot be negative.");

        if (OfficeId <= 0)
            return (false, "OfficeId is required.");

        if (!string.IsNullOrWhiteSpace(currentOffices)
            && !currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        return (true, null);
    }

    public LeadOwner ToModel(Guid organizationId) =>
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
            TypeOfProperty = TypeOfProperty,
            TellUsWhatYouLikeMostAboutYourProperty = TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = TellUsAnyDrawbacks,
            PreferredContactMethod = PreferredContactMethod,
            TimeDateForContact = TimeDateForContact,
            EmailPhoneConsent = EmailPhoneConsent,
            SmsConsent = SmsConsent,
            IsActive = IsActive
        };
}
