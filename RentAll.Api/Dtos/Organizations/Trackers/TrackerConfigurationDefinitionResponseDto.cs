namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerConfigurationDefinitionResponseDto
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
    public IEnumerable<TrackerDefinitionOptionResponseDto> Options { get; set; } = Enumerable.Empty<TrackerDefinitionOptionResponseDto>();
}
