namespace RentAll.Api.Dtos.Reports;

public class GetRecapReportDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public Guid? ReservationId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IncludeVoided { get; set; }
    public bool IncludeUnposted { get; set; } = true;
    public string RecapCategory { get; set; } = string.Empty;

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

    public JournalEntryRecapGetCriteria ToCriteria(Guid organizationId)
    {
        return new JournalEntryRecapGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = ResolvedOfficeIds,
            PropertyId = PropertyId,
            ReservationId = ReservationId,
            StartDate = StartDate,
            EndDate = EndDate,
            IncludeVoided = IncludeVoided,
            IncludeUnposted = IncludeUnposted,
            RecapCategory = (RecapCategory ?? string.Empty).Trim()
        };
    }
}
