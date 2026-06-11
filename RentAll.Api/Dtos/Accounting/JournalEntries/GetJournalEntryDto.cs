namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class GetJournalEntryDto
{
    public int[] OfficeIds { get; set; } = [];
    public int? SourceTypeId { get; set; }
    public Guid? SourceId { get; set; }
    public int? TransactionTypeId { get; set; }
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        return (true, null);
    }
}
