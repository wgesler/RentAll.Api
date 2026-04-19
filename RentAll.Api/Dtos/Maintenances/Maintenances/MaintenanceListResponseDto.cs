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
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }
    public bool PetsAllowed { get; set; }
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
        BedroomId1 = (int)maintenance.Bedroom1;
        BedroomId2 = (int)maintenance.Bedroom2;
        BedroomId3 = (int)maintenance.Bedroom3;
        BedroomId4 = (int)maintenance.Bedroom4;
        PetsAllowed = maintenance.PetsAllowed;
        Notes = maintenance.Notes;
        IsActive = maintenance.IsActive;
        IsDeleted = maintenance.IsDeleted;
        CreatedOn = maintenance.CreatedOn;
        CreatedBy = maintenance.CreatedBy;
        ModifiedOn = maintenance.ModifiedOn;
        ModifiedBy = maintenance.ModifiedBy;
    }
}
