namespace RentAll.Api.Dtos.Reports;

public class GetEscrowReportDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Cushion { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (StartDate.HasValue && EndDate < StartDate.Value)
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
            StartDate = StartDate,
            EndDate = EndDate,
            IncludeVoided = false,
            IncludeUnposted = true
        };
    }
}
