namespace RentAll.Api.Dtos.Maintenances.Maintenances;

public class MaintenanceListResponseDto
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string InspectionCheckList { get; set; } = string.Empty;
    public Guid? CleanerUserId { get; set; }
    public Guid? InspectorUserId { get; set; }
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }
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
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public MaintenanceListResponseDto(MaintenanceList maintenance)
    {
        MaintenanceId = maintenance.MaintenanceId;
        OrganizationId = maintenance.OrganizationId;
        OfficeId = maintenance.OfficeId;
        OfficeName = maintenance.OfficeName;
        PropertyId = maintenance.PropertyId;
        PropertyCode = maintenance.PropertyCode;
        InspectionCheckList = maintenance.InspectionCheckList;
        CleanerUserId = maintenance.CleanerUserId;
        InspectorUserId = maintenance.InspectorUserId;
        BedroomId1 = (int)maintenance.Bedroom1;
        BedroomId2 = (int)maintenance.Bedroom2;
        BedroomId3 = (int)maintenance.Bedroom3;
        BedroomId4 = (int)maintenance.Bedroom4;
        FilterDescription = maintenance.FilterDescription;
        LastFilterChangeDate = maintenance.LastFilterChangeDate;
        SmokeDetectors = maintenance.SmokeDetectors;
        LastSmokeChangeDate = maintenance.LastSmokeChangeDate;
        SmokeDetectorBatteries = maintenance.SmokeDetectorBatteries;
        LastDetectorChangeDate = maintenance.LastDetectorChangeDate;
        LicenseNo = maintenance.LicenseNo;
        LicenseDate = maintenance.LicenseDate;
        HvacNotes = maintenance.HvacNotes;
        HvacServiced = maintenance.HvacServiced;
        FireplaceNotes = maintenance.FireplaceNotes;
        FireplaceServiced = maintenance.FireplaceServiced;
        Notes = maintenance.Notes;
        IsActive = maintenance.IsActive;
        IsDeleted = maintenance.IsDeleted;
        CreatedOn = maintenance.CreatedOn;
        CreatedBy = maintenance.CreatedBy;
        ModifiedOn = maintenance.ModifiedOn;
        ModifiedBy = maintenance.ModifiedBy;
    }
}
