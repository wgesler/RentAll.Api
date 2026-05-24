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
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
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
            Notes = e.Notes,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName,
            QuotePath = e.QuotePath,
            INeedAsap = e.INeedAsap,
            EmailPhoneConsent = e.EmailPhoneConsent,
            SmsConsent = e.SmsConsent,
            IsActive = e.IsActive
        };

    private static LeadOwner ConvertOwnerEntityToModel(OwnerEntity e) =>
        new()
        {
            OwnerId = e.OwnerId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
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
            PurchaseKitchenItems = e.PurchaseKitchenItems,
            KitchenBudget = e.KitchenBudget,
            FurnishUnit = e.FurnishUnit,
            FurnishBudget = e.FurnishBudget,
            NumberOfBeds = e.NumberOfBeds,
            NumberOfBaths = e.NumberOfBaths,
            ApproxSqFootage = e.ApproxSqFootage,
            TypeOfProperty = e.PropertyTypeId.HasValue ? (PropertyType?)e.PropertyTypeId.Value : null,
            PropertyCode = e.PropertyCode,
            PropertyOffice = e.PropertyOffice,
            TellUsWhatYouLikeMostAboutYourProperty = e.TellUsWhatYouLikeMostAboutYourProperty,
            TellUsAnyDrawbacks = e.TellUsAnyDrawbacks,
            PreferredContactMethod = e.PreferredContactMethod,
            TimeDateForContact = e.TimeDateForContact,
            Notes = e.Notes,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName,
            EmailPhoneConsent = e.EmailPhoneConsent,
            SmsConsent = e.SmsConsent,
            IsActive = e.IsActive
        };

    private static LeadOwnerFormShare ConvertOwnerFormShareEntityToModel(OwnerFormShareEntity e) =>
        new()
        {
            ShareId = e.ShareId,
            OwnerId = e.OwnerId,
            OrganizationId = e.OrganizationId,
            TokenHash = e.TokenHash,
            ExpiresOn = e.ExpiresOn
        };

    private static OwnerHtml ConvertOwnerHtmlEntityToModel(OwnerHtmlEntity e) =>
        new()
        {
            PropertyId = e.PropertyId,
            OrganizationId = e.OrganizationId,
            OwnerAgreement = e.OwnerAgreement,
            DirectDeposit = e.DirectDeposit,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };

    private static OwnerAgreementInformation ConvertOwnerAgreementInformationEntityToModel(OwnerAgreementInformationEntity e) =>
        new()
        {
            OwnerAgreementInformationId = e.OwnerAgreementInformationId,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            OrganizationId = e.OrganizationId,
            AgreementIntroduction = e.AgreementIntroduction,
            Recitals = e.Recitals,
            SectionOneEmployment = e.SectionOneEmployment,
            SectionTwoAgentDuties = e.SectionTwoAgentDuties,
            SectionThreeOwnersDuties = e.SectionThreeOwnersDuties,
            SectionFourAdvertisingAndPromotion = e.SectionFourAdvertisingAndPromotion,
            SectionFiveMaintenanceRepairsAndOperations = e.SectionFiveMaintenanceRepairsAndOperations,
            SectionSixReimbursements = e.SectionSixReimbursements,
            SectionSevenGovernmentRegulations = e.SectionSevenGovernmentRegulations,
            SectionEightInsurance = e.SectionEightInsurance,
            SectionNineCollectionOfIncomeAndInstitutionOfLegalAction = e.SectionNineCollectionOfIncomeAndInstitutionOfLegalAction,
            SectionTenBankAccounts = e.SectionTenBankAccounts,
            SectionElevenRecordsAndReports = e.SectionElevenRecordsAndReports,
            SectionTwelveAdditionalDutiesAndRights = e.SectionTwelveAdditionalDutiesAndRights,
            SectionThirteenTerminationAndRenewal = e.SectionThirteenTerminationAndRenewal,
            SectionFourteenSaleOfPropertyAccess = e.SectionFourteenSaleOfPropertyAccess,
            SectionFifteenSummaryOfFees = e.SectionFifteenSummaryOfFees,
            SectionSixteenForeignOwnership = e.SectionSixteenForeignOwnership,
            SectionSeventeenIndemnity = e.SectionSeventeenIndemnity,
            SectionEighteenMiscellaneous = e.SectionEighteenMiscellaneous,
            SectionNineteenAdditionalForms = e.SectionNineteenAdditionalForms,
            InWitnessWhereof = e.InWitnessWhereof,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };

    private static OwnerInventoryInformation ConvertOwnerInventoryInformationEntityToModel(OwnerInventoryInformationEntity e) =>
        new()
        {
            OwnerId = e.OwnerId,
            OrganizationId = e.OrganizationId,
            OnSiteComplexManagementPhone = e.OnSiteComplexManagementPhone,
            KeyCount = e.KeyCount,
            GarageRemoteModelCode = e.GarageRemoteModelCode,
            StorageAccessDetails = e.StorageAccessDetails,
            CableSupplier = e.CableSupplier,
            CablePhone = e.CablePhone,
            CableAccountNumber = e.CableAccountNumber,
            ElectricSupplier = e.ElectricSupplier,
            ElectricPhone = e.ElectricPhone,
            ElectricAccountNumber = e.ElectricAccountNumber,
            InternetSupplier = e.InternetSupplier,
            InternetPhone = e.InternetPhone,
            InternetAccountNumber = e.InternetAccountNumber,
            FuseBoxLocation = e.FuseBoxLocation,
            SchoolDistrict = e.SchoolDistrict,
            LocalEmergencyContact = e.LocalEmergencyContact,
            AccessInformation = e.AccessInformation,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };

    private static LeadGeneral ConvertGeneralEntityToModel(GeneralEntity e) =>
        new()
        {
            GeneralId = e.GeneralId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            LeadState = (LeadStateType)e.LeadStateId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            PhoneMobile = e.PhoneMobile,
            Message = e.Message,
            Notes = e.Notes,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName,
            IsActive = e.IsActive
        };
}
