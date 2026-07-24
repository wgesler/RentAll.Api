namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class GetOwnerApAgingJournalEntryLinesDto
{
    public int[] OfficeIds { get; set; } = [];
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public DateOnly? EndDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        return (true, null);
    }
}
