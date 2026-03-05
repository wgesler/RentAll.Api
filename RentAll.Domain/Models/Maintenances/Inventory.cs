namespace RentAll.Domain.Models;

public class Inventory
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
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;
}
