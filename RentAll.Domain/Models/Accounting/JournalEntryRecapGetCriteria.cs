namespace RentAll.Domain.Models;

public class JournalEntryRecapGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public string RecapCategory { get; set; } = string.Empty;
}
