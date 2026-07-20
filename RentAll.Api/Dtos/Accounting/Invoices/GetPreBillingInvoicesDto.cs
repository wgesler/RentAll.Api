namespace RentAll.Api.Dtos.Accounting.Invoices;

public class GetPreBillingInvoicesDto
{
    public int[] OfficeIds { get; set; } = [];
    public DateOnly BillingMonth { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (BillingMonth == default)
            return (false, "BillingMonth is required");

        if (BillingMonth.Day != 1)
            return (false, "BillingMonth must be the first day of the month");

        return (true, null);
    }
}
