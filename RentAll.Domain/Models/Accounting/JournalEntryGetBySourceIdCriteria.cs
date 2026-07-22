namespace RentAll.Domain.Models;

public class JournalEntryGetBySourceIdCriteria
{
    public Guid OrganizationId { get; set; }
    public int SourceTypeId { get; set; }
    public Guid SourceId { get; set; }
    public string? OfficeIds { get; set; }
    public int? JournalEntryKindId { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public bool IncludeCashOnly { get; set; } = true;
}
