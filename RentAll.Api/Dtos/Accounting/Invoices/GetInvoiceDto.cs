namespace RentAll.Api.Dtos.Accounting.Invoices;

public class GetInvoiceDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? ReservationId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? InvoiceCode { get; set; }
    public bool IncludeInactive { get; set; }
    public bool IncludePaid { get; set; }
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
