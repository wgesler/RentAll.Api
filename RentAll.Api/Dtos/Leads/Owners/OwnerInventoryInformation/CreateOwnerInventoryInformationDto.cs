using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class CreateOwnerInventoryInformationDto
{
    public int OwnerId { get; set; }
    public Guid OrganizationId { get; set; }
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
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OwnerId <= 0)
            return (false, "OwnerId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        return (true, null);
    }

    public OwnerInventoryInformation ToModel(Guid currentUser)
    {
        return new OwnerInventoryInformation
        {
            OwnerId = OwnerId,
            OrganizationId = OrganizationId,
            OnSiteComplexManagementPhone = OnSiteComplexManagementPhone,
            KeyCount = KeyCount,
            GarageRemoteModelCode = GarageRemoteModelCode,
            StorageAccessDetails = StorageAccessDetails,
            CableSupplier = CableSupplier,
            CablePhone = CablePhone,
            CableAccountNumber = CableAccountNumber,
            ElectricSupplier = ElectricSupplier,
            ElectricPhone = ElectricPhone,
            ElectricAccountNumber = ElectricAccountNumber,
            InternetSupplier = InternetSupplier,
            InternetPhone = InternetPhone,
            InternetAccountNumber = InternetAccountNumber,
            FuseBoxLocation = FuseBoxLocation,
            SchoolDistrict = SchoolDistrict,
            LocalEmergencyContact = LocalEmergencyContact,
            AccessInformation = AccessInformation,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}
