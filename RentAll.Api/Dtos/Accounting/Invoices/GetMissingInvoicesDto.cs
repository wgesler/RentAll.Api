namespace RentAll.Api.Dtos.Accounting.Invoices;

public class GetMissingInvoicesDto
{
    public int[] OfficeIds { get; set; } = [];

    /// <summary>When true (default), non-ignored mismatches only; when false, all billed rows for active reservations through the current month.</summary>
    public bool MissingOnly { get; set; } = true;

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        return (true, null);
    }
}
