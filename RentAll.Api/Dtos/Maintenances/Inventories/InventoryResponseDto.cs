using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Inventories;

public class InventoryResponseDto
{
    public int InventoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; }
    public Guid MaintenanceId { get; set; }
    public string? InventoryCheckList { get; set; }
    public string? DocumentPath { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public InventoryResponseDto(Inventory inventory)
    {
        InventoryId = inventory.InventoryId;
        OrganizationId = inventory.OrganizationId;
        OfficeId = inventory.OfficeId;
        OfficeName = inventory.OfficeName;
        PropertyId = inventory.PropertyId;
        PropertyCode = inventory.PropertyCode;
        MaintenanceId = inventory.MaintenanceId;
        InventoryCheckList = inventory.InventoryCheckList;
        DocumentPath = inventory.DocumentPath;
        IsActive = inventory.IsActive;
        ModifiedOn = inventory.ModifiedOn;
        ModifiedBy = inventory.ModifiedByName;
    }
}
