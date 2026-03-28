namespace RentAll.Infrastructure.Entities.Maintenances;

public class MaintenanceEntity
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string InspectionCheckList { get; set; } = string.Empty;

    // Maintenance Section
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
}
