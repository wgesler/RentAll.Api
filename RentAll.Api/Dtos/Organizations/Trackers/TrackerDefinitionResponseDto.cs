namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerDefinitionResponseDto
{
    public Guid TrackerDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int TrackerContextId { get; set; }
    public string TrackerContextCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public TrackerDefinitionResponseDto(TrackerDefinition trackerDefinition)
    {
        TrackerDefinitionId = trackerDefinition.TrackerDefinitionId;
        OrganizationId = trackerDefinition.OrganizationId;
        OfficeId = trackerDefinition.OfficeId;
        OfficeName = trackerDefinition.OfficeName;
        TrackerContextId = (int)trackerDefinition.TrackerContextId;
        TrackerContextCode = trackerDefinition.TrackerContextCode;
        DisplayName = trackerDefinition.DisplayName;
        Description = trackerDefinition.Description;
        SortOrder = trackerDefinition.SortOrder;
        IsActive = trackerDefinition.IsActive;
        CreatedOn = trackerDefinition.CreatedOn;
        CreatedBy = trackerDefinition.CreatedBy;
        ModifiedOn = trackerDefinition.ModifiedOn;
        ModifiedBy = trackerDefinition.ModifiedBy;
    }
}

