namespace RentAll.Domain.Models;

public class EscrowReportJournalEntryDrillDownCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IncludeUnposted { get; set; }
}
