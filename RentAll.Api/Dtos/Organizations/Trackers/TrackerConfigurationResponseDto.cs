namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerConfigurationResponseDto
{
    public IEnumerable<TrackerConfigurationContextResponseDto> Contexts { get; set; } = Enumerable.Empty<TrackerConfigurationContextResponseDto>();
}
