using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class TrackerContext
{
    public TrackerContextType TrackerContextId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

