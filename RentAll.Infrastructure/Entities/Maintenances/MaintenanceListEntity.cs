namespace RentAll.Infrastructure.Entities.Maintenances;

public class MaintenanceListEntity
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string InspectionCheckList { get; set; } = string.Empty;
    public Guid? CleanerUserId { get; set; }
    public DateOnly? CleaningDate { get; set; }
    public Guid? InspectorUserId { get; set; }
    public DateOnly? InspectingDate { get; set; }
    public Guid? CarpetUserId { get; set; }
    public DateOnly? CarpetDate { get; set; }
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }
    public bool PetsAllowed { get; set; }

    // Maintenance Section
    public string? FilterDescription { get; set; }
    public DateOnly? LastFilterChangeDate { get; set; }
    public string? SmokeDetectors { get; set; }
    public DateOnly? LastSmokeChangeDate { get; set; }
    public string? SmokeDetectorBatteries { get; set; }
    public DateOnly? LastBatteryChangeDate { get; set; }
    public string? LicenseNo { get; set; }
    public DateOnly? LicenseDate { get; set; }
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
