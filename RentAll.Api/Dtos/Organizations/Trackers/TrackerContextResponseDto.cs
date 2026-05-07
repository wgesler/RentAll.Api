namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerContextResponseDto
{
    public int TrackerContextId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public TrackerContextResponseDto(TrackerContext trackerContext)
    {
        TrackerContextId = (int)trackerContext.TrackerContextId;
        Code = trackerContext.Code;
        Description = trackerContext.Description;
        IsActive = trackerContext.IsActive;
    }
}

