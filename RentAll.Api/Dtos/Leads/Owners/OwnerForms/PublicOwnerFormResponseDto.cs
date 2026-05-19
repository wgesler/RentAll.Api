using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class PublicOwnerFormResponseDto
{
    public int OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public PublicOwnerFormDto Form { get; set; } = new();

    public PublicOwnerFormResponseDto(LeadOwner owner, OwnerInventoryInformation? ownerInventoryInformation, DateTimeOffset expiresOn)
    {
        OwnerId = owner.OwnerId;
        OwnerName = $"{owner.FirstName} {owner.LastName}".Trim();
        ExpiresOn = expiresOn;
        Form = new PublicOwnerFormDto(owner, ownerInventoryInformation);
    }
}

public class PublicOwnerFormDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LocationOfProperty { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public decimal? AdjustedGrossRentTarget { get; set; }
    public decimal? OnlineFeeRentReady { get; set; }
    public decimal? OnlineCleanHourlyFee { get; set; }
    public decimal? WorkingBalanceEscrow { get; set; }
    public decimal? AnnualLinenCustomAmount { get; set; }
    public decimal? OfflineFee { get; set; }
    public bool FurnishingKitchenItemsRequested { get; set; }
    public decimal? FurnishingKitchenItemsAmount { get; set; }
    public bool FurnishingFullUnitRequested { get; set; }
    public decimal? FurnishingFullUnitEstimateAmount { get; set; }
    public bool AnnualLinenTierStudio1Bedroom { get; set; }
    public bool AnnualLinenTier2Bedroom { get; set; }
    public bool AnnualLinenTier3Bedroom { get; set; }
    public string? NumberOfBeds { get; set; }
    public string? NumberOfBaths { get; set; }
    public string? ApproxSqFootage { get; set; }
    public string? TypeOfProperty { get; set; }
    public string? PropertyCode { get; set; }
    public string? PropertyOffice { get; set; }
    public string? PropertyGoals { get; set; }
    public string? TellUsMoreAboutYourGoals { get; set; }
    public string? TellUsMoreAboutProperty { get; set; }
    public string? TellUsWhatYouLikeMostAboutYourProperty { get; set; }
    public string? TellUsAnyDrawbacks { get; set; }
    public string? PreferredContactMethod { get; set; }
    public string? TimeDateForContact { get; set; }
    public bool EmailPhoneConsent { get; set; }
    public bool SmsConsent { get; set; }
    public string? OnSiteComplexManagementPhone { get; set; }
    public string? KeyCount { get; set; }
    public string? GarageRemoteModelCode { get; set; }
    public string? StorageAccessDetails { get; set; }
    public string? CableSupplier { get; set; }
    public string? CablePhone { get; set; }
    public string? CableAccountNumber { get; set; }
    public string? ElectricSupplier { get; set; }
    public string? ElectricPhone { get; set; }
    public string? ElectricAccountNumber { get; set; }
    public string? InternetSupplier { get; set; }
    public string? InternetPhone { get; set; }
    public string? InternetAccountNumber { get; set; }
    public string? FuseBoxLocation { get; set; }
    public string? SchoolDistrict { get; set; }
    public string? LocalEmergencyContact { get; set; }
    public string? AccessInformation { get; set; }

    public PublicOwnerFormDto()
    {
    }

    public PublicOwnerFormDto(LeadOwner owner, OwnerInventoryInformation? ownerInventoryInformation)
    {
        FirstName = owner.FirstName;
        LastName = owner.LastName;
        Email = owner.Email;
        Phone = owner.Phone;
        LocationOfProperty = owner.LocationOfProperty;
        Address = owner.Address;
        City = owner.City;
        State = owner.State;
        Zip = owner.Zip;
        AdjustedGrossRentTarget = owner.AdjustedGrossRentTarget;
        OnlineFeeRentReady = owner.OnlineFee;
        OnlineCleanHourlyFee = owner.OnlineClean;
        WorkingBalanceEscrow = owner.WorkingBalance;
        AnnualLinenCustomAmount = owner.AnnualLinenAmount;
        OfflineFee = owner.OfflineFee;
        FurnishingKitchenItemsRequested = owner.PurchaseKitchenItems;
        FurnishingKitchenItemsAmount = owner.KitchenBudget;
        FurnishingFullUnitRequested = owner.FurnishUnit;
        FurnishingFullUnitEstimateAmount = owner.FurnishBudget;
        AnnualLinenTierStudio1Bedroom = owner.OneBedroom;
        AnnualLinenTier2Bedroom = owner.TwoBedroom;
        AnnualLinenTier3Bedroom = owner.ThreeBedroom;
        NumberOfBeds = owner.NumberOfBeds;
        NumberOfBaths = owner.NumberOfBaths;
        ApproxSqFootage = owner.ApproxSqFootage;
        TypeOfProperty = owner.TypeOfProperty;
        PropertyCode = owner.PropertyCode;
        PropertyOffice = owner.PropertyOffice;
        PropertyGoals = owner.PropertyGoals;
        TellUsMoreAboutYourGoals = owner.TellUsMoreAboutYourGoals;
        TellUsMoreAboutProperty = owner.TellUsMoreAboutProperty;
        TellUsWhatYouLikeMostAboutYourProperty = owner.TellUsWhatYouLikeMostAboutYourProperty;
        TellUsAnyDrawbacks = owner.TellUsAnyDrawbacks;
        PreferredContactMethod = owner.PreferredContactMethod;
        TimeDateForContact = owner.TimeDateForContact;
        EmailPhoneConsent = owner.EmailPhoneConsent;
        SmsConsent = owner.SmsConsent;
        OnSiteComplexManagementPhone = ownerInventoryInformation?.OnSiteComplexManagementPhone;
        KeyCount = ownerInventoryInformation?.KeyCount;
        GarageRemoteModelCode = ownerInventoryInformation?.GarageRemoteModelCode;
        StorageAccessDetails = ownerInventoryInformation?.StorageAccessDetails;
        CableSupplier = ownerInventoryInformation?.CableSupplier;
        CablePhone = ownerInventoryInformation?.CablePhone;
        CableAccountNumber = ownerInventoryInformation?.CableAccountNumber;
        ElectricSupplier = ownerInventoryInformation?.ElectricSupplier;
        ElectricPhone = ownerInventoryInformation?.ElectricPhone;
        ElectricAccountNumber = ownerInventoryInformation?.ElectricAccountNumber;
        InternetSupplier = ownerInventoryInformation?.InternetSupplier;
        InternetPhone = ownerInventoryInformation?.InternetPhone;
        InternetAccountNumber = ownerInventoryInformation?.InternetAccountNumber;
        FuseBoxLocation = ownerInventoryInformation?.FuseBoxLocation;
        SchoolDistrict = ownerInventoryInformation?.SchoolDistrict;
        LocalEmergencyContact = ownerInventoryInformation?.LocalEmergencyContact;
        AccessInformation = ownerInventoryInformation?.AccessInformation;
    }
}
