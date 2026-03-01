using RentAll.Domain.Models.Maintenances;

namespace RentAll.Api.Dtos.Maintenances.Inspections;

public class UpdateInspectionDto
{
    public int InspectionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string? InspectionCheckList { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (InspectionId <= 0)
            return (false, "InspectionId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (MaintenanceId == Guid.Empty)
            return (false, "MaintenanceId is required");

        return (true, null);
    }

    public Inspection ToModel(Guid currentUser)
    {
        return new Inspection
        {
            InspectionId = InspectionId,
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            MaintenanceId = MaintenanceId,
            InspectionCheckList = InspectionCheckList,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
