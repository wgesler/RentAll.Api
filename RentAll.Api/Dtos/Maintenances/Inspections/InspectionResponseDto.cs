namespace RentAll.Api.Dtos.Maintenances.Inspections;

public class InspectionResponseDto
{
    public int InspectionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; }
    public int InspectionTypeId { get; set; }
    public string? InspectionCheckList { get; set; }
    public string? DocumentPath { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public string ModifiedBy { get; set; }

    public InspectionResponseDto(Inspection inspection)
    {
        InspectionId = inspection.InspectionId;
        OrganizationId = inspection.OrganizationId;
        OfficeId = inspection.OfficeId;
        OfficeName = inspection.OfficeName;
        PropertyId = inspection.PropertyId;
        PropertyCode = inspection.PropertyCode;
        InspectionTypeId = (int)inspection.InspectionType;
        InspectionCheckList = inspection.InspectionCheckList;
        DocumentPath = inspection.DocumentPath;
        IsActive = inspection.IsActive;
        ModifiedOn = inspection.ModifiedOn;
        ModifiedBy = inspection.ModifiedByName;
    }
}
