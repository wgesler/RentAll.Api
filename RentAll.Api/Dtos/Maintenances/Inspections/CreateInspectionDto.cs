namespace RentAll.Api.Dtos.Maintenances.Inspections;

public class CreateInspectionDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public int InspectionTypeId { get; set; }
    public string? InspectionCheckList { get; set; }
    public string? DocumentPath { get; set; }
    public bool IsActive { get; set; } = true;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (!Enum.IsDefined(typeof(InspectionType), InspectionTypeId))
            return (false, $"Invalid InspectionType value: {InspectionTypeId}");

        return (true, null);
    }

    public Inspection ToModel(Guid currentUser)
    {
        return new Inspection
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PropertyId = PropertyId,
            InspectionType = (InspectionType)InspectionTypeId,
            InspectionCheckList = InspectionCheckList,
            DocumentPath = DocumentPath,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}
