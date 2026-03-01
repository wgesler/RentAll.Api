using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Maintenances;

public class MaintenanceResponseDto
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string InspectionCheckList { get; set; } = string.Empty;
    public string InventoryCheckList { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public MaintenanceResponseDto(Maintenance maintenanceRecord)
    {
        MaintenanceId = maintenanceRecord.MaintenanceId;
        OrganizationId = maintenanceRecord.OrganizationId;
        OfficeId = maintenanceRecord.OfficeId;
        OfficeName = maintenanceRecord.OfficeName;
        PropertyId = maintenanceRecord.PropertyId;
        InspectionCheckList = maintenanceRecord.InspectionCheckList;
        InventoryCheckList = maintenanceRecord.InventoryCheckList;
        Notes = maintenanceRecord.Notes;
        IsActive = maintenanceRecord.IsActive;
        CreatedOn = maintenanceRecord.CreatedOn;
        CreatedBy = maintenanceRecord.CreatedBy;
        ModifiedOn = maintenanceRecord.ModifiedOn;
        ModifiedBy = maintenanceRecord.ModifiedBy;
    }
}
