namespace RentAll.Domain.Models;

public class JournalEntryLineOwnerApAgingGetCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public string ChartOfAccountIds { get; set; } = string.Empty;
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public DateOnly? EndDate { get; set; }
}
