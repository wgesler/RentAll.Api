namespace RentAll.Api.Dtos.Organizations.Trackers;

public class TrackerContextUpdateDto
{
    public int TrackerContextId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (TrackerContextId <= 0)
            return (false, "TrackerContextId is required");

        if (string.IsNullOrWhiteSpace(Code))
            return (false, "Code is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        return (true, null);
    }

    public TrackerContext ToModel()
    {
        return new TrackerContext
        {
            TrackerContextId = (TrackerContextType)TrackerContextId,
            Code = Code,
            Description = Description,
            IsActive = IsActive
        };
    }
}

