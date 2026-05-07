namespace RentAll.Infrastructure.Entities.Organizations;

public class TrackerContextEntity
{
    public int TrackerContextId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

