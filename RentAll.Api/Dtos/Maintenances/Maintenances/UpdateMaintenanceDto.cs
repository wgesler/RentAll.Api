using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Maintenances.Maintenances;

public class UpdateMaintenanceDto
{
    public Guid MaintenanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string InspectionCheckList { get; set; } = string.Empty;
    public Guid? CleanerUserId { get; set; }
    public DateTimeOffset? CleaningDate { get; set; }
    public Guid? InspectorUserId { get; set; }
    public DateTimeOffset? InspectingDate { get; set; }
    public Guid? CarpetUserId { get; set; }
    public DateTimeOffset? CarpetDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (MaintenanceId == Guid.Empty)
            return (false, "MaintenanceId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(InspectionCheckList))
            return (false, "InspectionCheckList is required");

        if (!string.IsNullOrWhiteSpace(Notes) && Notes.Length > 500)
            return (false, "Notes must be 500 characters or less");

        return (true, null);
    }

    public Maintenance ToModel(Guid currentUser)
    {
        return new Maintenance
        {
            MaintenanceId = MaintenanceId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            InspectionCheckList = InspectionCheckList,
            CleanerUserId = CleanerUserId,
            CleaningDate = CleaningDate,
            InspectorUserId = InspectorUserId,
            InspectingDate = InspectingDate,
            CarpetUserId = CarpetUserId,
            CarpetDate = CarpetDate,
            Notes = Notes,
            IsActive = IsActive,
            IsDeleted = IsDeleted,
            ModifiedBy = currentUser
        };
    }
}
