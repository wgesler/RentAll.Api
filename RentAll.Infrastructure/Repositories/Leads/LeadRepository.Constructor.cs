using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;
using RentAll.Infrastructure.Entities.Leads;

namespace RentAll.Infrastructure.Repositories.Leads;

public partial class LeadRepository : ILeadRepository
{
    private readonly string _dbConnectionString;

    public LeadRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value.Date) : null;

    private static DateTime? ToSqlDate(DateOnly? value) =>
        value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;

    private static LeadRental ConvertRentalEntityToModel(RentalEntity e) =>
        new()
        {
            RentalId = e.RentalId,
            LeadState = (LeadStateType)e.LeadStateId,
            AgentId = e.AgentId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            DesiredLocation = e.DesiredLocation,
            PropertyRefId = e.PropertyRefId,
            EstimatedArrivalDate = e.EstimatedArrivalDate,
            EstimatedDepartureDate = e.EstimatedDepartureDate,
            MaxMonthlyBudget = e.MaxMonthlyBudget,
            MinBedrooms = e.MinBedrooms,
            NumberOfOccupants = e.NumberOfOccupants,
            WhatBringsYouToTown = e.WhatBringsYouToTown,
            HowDidYouFindUs = e.HowDidYouFindUs,
            TellUsMoreAboutHowYouFoundUs = e.TellUsMoreAboutHowYouFoundUs,
            PetFriendly = e.PetFriendly,
            DecisionDate = ToDateOnly(e.DecisionDate),
            OrganizationName = e.OrganizationName,
            AdditionalInformation = e.AdditionalInformation,
            INeedAsap = e.INeedAsap,
            EmailPhoneConsent = e.EmailPhoneConsent,
            SmsConsent = e.SmsConsent,
            IsActive = e.IsActive
        };

    private static LeadOwner ConvertOwnerEntityToModel(OwnerEntity e) =>
        new()
        {
            OwnerId = e.OwnerId,
            LeadState = (LeadStateType)e.LeadStateId,
            AgentId = e.AgentId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            LocationOfProperty = e.LocationOfProperty,
            ProgramInterest = e.ProgramInterest,
            WhatIsPromptingContact = e.WhatIsPromptingContact,
            TimeFrame = e.TimeFrame,
            TargetRentReadyDate = ToDateOnly(e.TargetRentReadyDate),
            PropertyGoals = e.PropertyGoals,
            TellUsMoreAboutYourGoals = e.TellUsMoreAboutYourGoals,
            YearsOfExperienceWithRentals = e.YearsOfExperienceWithRentals,
            TellUsMoreAboutProperty = e.TellUsMoreAboutProperty,
            Address = e.Address,
            City = e.City,
            State = e.State,
            Zip = e.Zip,
            NumberOfBeds = e.NumberOfBeds,
            NumberOfBaths = e.NumberOfBaths,
            ApproxSqFootage = e.ApproxSqFootage,
            TypeOfProperty = e.TypeOfProperty,
            TellUsWhatYouLikeMostAboutYourProperty = e.TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = e.TellUsAnyDrawbacks,
            PreferredContactMethod = e.PreferredContactMethod,
            TimeDateForContact = e.TimeDateForContact,
            EmailPhoneConsent = e.EmailPhoneConsent,
            SmsConsent = e.SmsConsent,
            IsActive = e.IsActive
        };
}
