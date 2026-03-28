using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class MaintenanceList
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
    public BedSizeType Bedroom1 { get; set; }
    public BedSizeType Bedroom2 { get; set; }
    public BedSizeType Bedroom3 { get; set; }
    public BedSizeType Bedroom4 { get; set; }
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
