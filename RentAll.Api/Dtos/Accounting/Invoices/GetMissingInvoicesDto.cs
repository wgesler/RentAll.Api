namespace RentAll.Api.Dtos.Accounting.Invoices;

public class GetMissingInvoicesDto
{
    public int[] OfficeIds { get; set; } = [];

    /// <summary>When true, return only ignored billed mismatch rows; when false, only non-ignored.</summary>
    public bool IncludeIgnored { get; set; }

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
