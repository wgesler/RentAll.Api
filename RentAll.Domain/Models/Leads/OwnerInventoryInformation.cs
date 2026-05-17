namespace RentAll.Domain.Models.Leads;

public class OwnerInventoryInformation
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
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
