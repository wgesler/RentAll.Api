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
    public BedSizeType Bedroom1 { get; set; }
    public BedSizeType Bedroom2 { get; set; }
    public BedSizeType Bedroom3 { get; set; }
    public BedSizeType Bedroom4 { get; set; }
    public bool PetsAllowed { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
