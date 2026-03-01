namespace RentAll.Infrastructure.Entities.Maintenances;

public class InventoryListEntity
{
    public int InventoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid MaintenanceId { get; set; }
    public string? InventoryCheckList { get; set; }
    public string? DocumentPath { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
