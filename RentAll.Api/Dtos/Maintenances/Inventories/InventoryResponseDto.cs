using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Inventories;

public class InventoryResponseDto
{
    public int InventoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string? InventoryCheckList { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public InventoryResponseDto(Inventory inventory)
    {
        InventoryId = inventory.InventoryId;
        OrganizationId = inventory.OrganizationId;
        OfficeId = inventory.OfficeId;
        PropertyId = inventory.PropertyId;
        MaintenanceId = inventory.MaintenanceId;
        InventoryCheckList = inventory.InventoryCheckList;
        IsActive = inventory.IsActive;
        CreatedOn = inventory.CreatedOn;
        CreatedBy = inventory.CreatedBy;
        ModifiedOn = inventory.ModifiedOn;
        ModifiedBy = inventory.ModifiedBy;
    }
}
