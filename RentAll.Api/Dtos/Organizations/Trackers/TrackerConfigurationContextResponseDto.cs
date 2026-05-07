namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerConfigurationContextResponseDto
{
    public int TrackerContextId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IEnumerable<TrackerConfigurationDefinitionResponseDto> Definitions { get; set; } = Enumerable.Empty<TrackerConfigurationDefinitionResponseDto>();
}
