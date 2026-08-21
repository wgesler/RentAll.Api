namespace RentAll.Domain.Models;

public class UserGuide
{
    public Guid UserGuideId { get; set; }
    public Dictionary<string, string> Sections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
