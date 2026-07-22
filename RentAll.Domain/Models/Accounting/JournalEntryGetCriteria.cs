namespace RentAll.Domain.Models;

public class JournalEntryGetCriteria

{

    public Guid OrganizationId { get; set; }

    public string OfficeIds { get; set; } = string.Empty;

    public int? SourceTypeId { get; set; }

    public Guid? SourceId { get; set; }

    public bool IncludeVoided { get; set; }

    public bool IncludeUnposted { get; set; } = true;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

}
