namespace RentAll.Domain.Models;

public class OwnerReportJournalEntryDrillDownCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
