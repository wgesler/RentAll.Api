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
    public string? FilterDescription { get; set; }
    public DateTimeOffset? LastFilterChangeDate { get; set; }
    public string? SmokeDetectors { get; set; }
    public DateTimeOffset? LastSmokeChangeDate { get; set; }
    public string? SmokeDetectorBatteries { get; set; }
    public DateTimeOffset? LastDetectorChangeDate { get; set; }
    public string? LicenseNo { get; set; }
    public DateTimeOffset? LicenseDate { get; set; }
    public string? HvacNotes { get; set; }
    public DateTimeOffset? HvacServiced { get; set; }
    public string? FireplaceNotes { get; set; }
    public DateTimeOffset? FireplaceServiced { get; set; }
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
        FilterDescription = maintenanceRecord.FilterDescription;
        LastFilterChangeDate = maintenanceRecord.LastFilterChangeDate;
        SmokeDetectors = maintenanceRecord.SmokeDetectors;
        LastSmokeChangeDate = maintenanceRecord.LastSmokeChangeDate;
        SmokeDetectorBatteries = maintenanceRecord.SmokeDetectorBatteries;
        LastDetectorChangeDate = maintenanceRecord.LastDetectorChangeDate;
        LicenseNo = maintenanceRecord.LicenseNo;
        LicenseDate = maintenanceRecord.LicenseDate;
        HvacNotes = maintenanceRecord.HvacNotes;
        HvacServiced = maintenanceRecord.HvacServiced;
        FireplaceNotes = maintenanceRecord.FireplaceNotes;
        FireplaceServiced = maintenanceRecord.FireplaceServiced;
        Notes = maintenanceRecord.Notes;
        IsActive = maintenanceRecord.IsActive;
        CreatedOn = maintenanceRecord.CreatedOn;
        CreatedBy = maintenanceRecord.CreatedBy;
        ModifiedOn = maintenanceRecord.ModifiedOn;
        ModifiedBy = maintenanceRecord.ModifiedBy;
    }
}
