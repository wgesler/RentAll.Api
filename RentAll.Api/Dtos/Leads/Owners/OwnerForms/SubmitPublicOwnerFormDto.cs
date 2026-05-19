using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class SubmitPublicOwnerFormDto
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
    public string? AdjustedGrossRentTarget { get; set; }
    public string? OnlineFeeRentReady { get; set; }
    public string? OnlineCleanHourlyFee { get; set; }
    public string? WorkingBalanceEscrow { get; set; }
    public string? AnnualLinenCustomAmount { get; set; }
    public string? OfflineFee { get; set; }
    public bool FurnishingKitchenItemsRequested { get; set; }
    public string? FurnishingKitchenItemsAmount { get; set; }
    public bool FurnishingFullUnitRequested { get; set; }
    public string? FurnishingFullUnitEstimateAmount { get; set; }
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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

        return (true, null);
    }

    public void ApplyTo(LeadOwner owner)
    {
        owner.FirstName = FirstName;
        owner.LastName = LastName;
        owner.Email = Email;
        owner.Phone = Phone;
        owner.LocationOfProperty = LocationOfProperty;
        owner.Address = Address;
        owner.City = City;
        owner.State = State;
        owner.Zip = Zip;
        owner.AdjustedGrossRentTarget = ParseNullableDecimal(AdjustedGrossRentTarget);
        owner.OnlineFee = ParseNullableDecimal(OnlineFeeRentReady);
        owner.OnlineClean = ParseNullableDecimal(OnlineCleanHourlyFee);
        owner.WorkingBalance = ParseNullableDecimal(WorkingBalanceEscrow);
        owner.AnnualLinenAmount = ParseNullableDecimal(AnnualLinenCustomAmount);
        owner.OfflineFee = ParseNullableDecimal(OfflineFee);
        owner.PurchaseKitchenItems = FurnishingKitchenItemsRequested;
        owner.KitchenBudget = ParseNullableDecimal(FurnishingKitchenItemsAmount);
        owner.FurnishUnit = FurnishingFullUnitRequested;
        owner.FurnishBudget = ParseNullableDecimal(FurnishingFullUnitEstimateAmount);
        owner.OneBedroom = AnnualLinenTierStudio1Bedroom;
        owner.TwoBedroom = AnnualLinenTier2Bedroom;
        owner.ThreeBedroom = AnnualLinenTier3Bedroom;
        owner.NumberOfBeds = NumberOfBeds;
        owner.NumberOfBaths = NumberOfBaths;
        owner.ApproxSqFootage = ApproxSqFootage;
        owner.TypeOfProperty = TypeOfProperty;
        owner.PropertyCode = PropertyCode;
        owner.PropertyOffice = PropertyOffice;
        owner.PropertyGoals = PropertyGoals;
        owner.TellUsMoreAboutYourGoals = TellUsMoreAboutYourGoals;
        owner.TellUsMoreAboutProperty = TellUsMoreAboutProperty;
        owner.TellUsWhatYouLikeMostAboutYourProperty = TellUsWhatYouLikeMostAboutYourProperty;
        owner.TellUsAnyDrawbacks = TellUsAnyDrawbacks;
        owner.PreferredContactMethod = PreferredContactMethod;
        owner.TimeDateForContact = TimeDateForContact;
        owner.EmailPhoneConsent = EmailPhoneConsent;
        owner.SmsConsent = SmsConsent;
    }

    public void ApplyTo(OwnerInventoryInformation ownerInventoryInformation)
    {
        ownerInventoryInformation.OnSiteComplexManagementPhone = OnSiteComplexManagementPhone;
        ownerInventoryInformation.KeyCount = KeyCount;
        ownerInventoryInformation.GarageRemoteModelCode = GarageRemoteModelCode;
        ownerInventoryInformation.StorageAccessDetails = StorageAccessDetails;
        ownerInventoryInformation.CableSupplier = CableSupplier;
        ownerInventoryInformation.CablePhone = CablePhone;
        ownerInventoryInformation.CableAccountNumber = CableAccountNumber;
        ownerInventoryInformation.ElectricSupplier = ElectricSupplier;
        ownerInventoryInformation.ElectricPhone = ElectricPhone;
        ownerInventoryInformation.ElectricAccountNumber = ElectricAccountNumber;
        ownerInventoryInformation.InternetSupplier = InternetSupplier;
        ownerInventoryInformation.InternetPhone = InternetPhone;
        ownerInventoryInformation.InternetAccountNumber = InternetAccountNumber;
        ownerInventoryInformation.FuseBoxLocation = FuseBoxLocation;
        ownerInventoryInformation.SchoolDistrict = SchoolDistrict;
        ownerInventoryInformation.LocalEmergencyContact = LocalEmergencyContact;
        ownerInventoryInformation.AccessInformation = AccessInformation;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, out var parsed) ? parsed : null;
    }
}
