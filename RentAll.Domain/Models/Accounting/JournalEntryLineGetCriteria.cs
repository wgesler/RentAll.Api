namespace RentAll.Domain.Models;

public class JournalEntryLineGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public int? ChartOfAccountId { get; set; }
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? ContactId { get; set; }
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public bool UnclearedOnly { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
