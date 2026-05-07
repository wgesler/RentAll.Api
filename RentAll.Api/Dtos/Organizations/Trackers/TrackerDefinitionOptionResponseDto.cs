namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerDefinitionOptionResponseDto
{
    public Guid TrackerDefinitionOptionId { get; set; }
    public Guid TrackerDefinitionId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int TrackerContextId { get; set; }
    public string TrackerContextCode { get; set; } = string.Empty;
    public string TrackerDisplayName { get; set; } = string.Empty;
    public string? TrackerDescription { get; set; }
    public int TrackerSortOrder { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? OptionDescription { get; set; }
    public int OptionSortOrder { get; set; }
    public bool IsActive { get; set; }

    public TrackerDefinitionOptionResponseDto(TrackerDefinitionOption trackerDefinitionOption)
    {
        TrackerDefinitionOptionId = trackerDefinitionOption.TrackerDefinitionOptionId;
        TrackerDefinitionId = trackerDefinitionOption.TrackerDefinitionId;
        OrganizationId = trackerDefinitionOption.OrganizationId;
        OfficeId = trackerDefinitionOption.OfficeId;
        OfficeName = trackerDefinitionOption.OfficeName;
        TrackerContextId = (int)trackerDefinitionOption.TrackerContextId;
        TrackerContextCode = trackerDefinitionOption.TrackerContextCode;
        TrackerDisplayName = trackerDefinitionOption.TrackerDisplayName;
        TrackerDescription = trackerDefinitionOption.TrackerDescription;
        TrackerSortOrder = trackerDefinitionOption.TrackerSortOrder;
        Label = trackerDefinitionOption.Label;
        OptionDescription = trackerDefinitionOption.OptionDescription;
        OptionSortOrder = trackerDefinitionOption.OptionSortOrder;
        IsActive = trackerDefinitionOption.IsActive;
    }
}

