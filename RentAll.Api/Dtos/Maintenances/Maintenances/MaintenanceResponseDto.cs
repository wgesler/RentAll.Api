using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Maintenances;

public class MaintenanceResponseDto
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string InspectionCheckList { get; set; } = string.Empty;
    public Guid? CleanerUserId { get; set; }
    public DateTimeOffset? CleaningDate { get; set; }
    public Guid? InspectorUserId { get; set; }
    public DateTimeOffset? InspectingDate { get; set; }
    public Guid? CarpetUserId { get; set; }
    public DateTimeOffset? CarpetDate { get; set; }
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
        PropertyCode = maintenanceRecord.PropertyCode;
        InspectionCheckList = maintenanceRecord.InspectionCheckList;
        CleanerUserId = maintenanceRecord.CleanerUserId;
        CleaningDate = maintenanceRecord.CleaningDate;
        InspectorUserId = maintenanceRecord.InspectorUserId;
        InspectingDate = maintenanceRecord.InspectingDate;
        CarpetUserId = maintenanceRecord.CarpetUserId;
        CarpetDate = maintenanceRecord.CarpetDate;
        Notes = maintenanceRecord.Notes;
        IsActive = maintenanceRecord.IsActive;
        CreatedOn = maintenanceRecord.CreatedOn;
        CreatedBy = maintenanceRecord.CreatedBy;
        ModifiedOn = maintenanceRecord.ModifiedOn;
        ModifiedBy = maintenanceRecord.ModifiedBy;
    }
}
