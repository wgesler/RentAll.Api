using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Dtos.Leads.Owners;

public class OwnerInventoryInformationResponseDto
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
    public bool IsActive { get; set; }

    public OwnerInventoryInformationResponseDto(OwnerInventoryInformation ownerInventoryInformation)
    {
        OwnerId = ownerInventoryInformation.OwnerId;
        OrganizationId = ownerInventoryInformation.OrganizationId;
        OnSiteComplexManagementPhone = ownerInventoryInformation.OnSiteComplexManagementPhone;
        KeyCount = ownerInventoryInformation.KeyCount;
        GarageRemoteModelCode = ownerInventoryInformation.GarageRemoteModelCode;
        StorageAccessDetails = ownerInventoryInformation.StorageAccessDetails;
        CableSupplier = ownerInventoryInformation.CableSupplier;
        CablePhone = ownerInventoryInformation.CablePhone;
        CableAccountNumber = ownerInventoryInformation.CableAccountNumber;
        ElectricSupplier = ownerInventoryInformation.ElectricSupplier;
        ElectricPhone = ownerInventoryInformation.ElectricPhone;
        ElectricAccountNumber = ownerInventoryInformation.ElectricAccountNumber;
        InternetSupplier = ownerInventoryInformation.InternetSupplier;
        InternetPhone = ownerInventoryInformation.InternetPhone;
        InternetAccountNumber = ownerInventoryInformation.InternetAccountNumber;
        FuseBoxLocation = ownerInventoryInformation.FuseBoxLocation;
        SchoolDistrict = ownerInventoryInformation.SchoolDistrict;
        LocalEmergencyContact = ownerInventoryInformation.LocalEmergencyContact;
        AccessInformation = ownerInventoryInformation.AccessInformation;
        IsActive = ownerInventoryInformation.IsActive;
    }
}
