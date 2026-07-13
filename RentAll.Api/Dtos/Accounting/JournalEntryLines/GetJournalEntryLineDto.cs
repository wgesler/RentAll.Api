namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class GetJournalEntryLineDto
{
    public int[] OfficeIds { get; set; } = [];
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

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    /// <summary>Null or non-positive values mean all accounts.</summary>
    public int? ResolvedChartOfAccountId =>
        ChartOfAccountId is > 0 ? ChartOfAccountId : null;

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
