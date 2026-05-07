namespace RentAll.Infrastructure.Entities.Organizations;

public class TrackerDefinitionOptionEntity
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
}

