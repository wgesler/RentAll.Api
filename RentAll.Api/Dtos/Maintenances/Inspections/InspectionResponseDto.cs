using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Inspections;

public class InspectionResponseDto
{
    public int InspectionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string? InspectionCheckList { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public InspectionResponseDto(Inspection inspection)
    {
        InspectionId = inspection.InspectionId;
        OrganizationId = inspection.OrganizationId;
        OfficeId = inspection.OfficeId;
        PropertyId = inspection.PropertyId;
        MaintenanceId = inspection.MaintenanceId;
        InspectionCheckList = inspection.InspectionCheckList;
        IsActive = inspection.IsActive;
        CreatedOn = inspection.CreatedOn;
        CreatedBy = inspection.CreatedBy;
        ModifiedOn = inspection.ModifiedOn;
        ModifiedBy = inspection.ModifiedBy;
    }
}
