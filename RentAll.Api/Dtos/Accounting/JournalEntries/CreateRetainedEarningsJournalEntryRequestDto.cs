namespace RentAll.Api.Dtos.Accounting.JournalEntries;

public class CreateRetainedEarningsJournalEntryRequestDto
{
    public int OfficeId { get; set; }
    public int FiscalYearEndYear { get; set; } = 2024;

    public (bool IsValid, string? ErrorMessage) IsValid(string? officeAccess)
    {
        if (OfficeId <= 0)
            return (false, "Office ID is required.");

        if (FiscalYearEndYear < 1900 || FiscalYearEndYear > 9999)
            return (false, "Fiscal year end year is invalid.");

        if (!string.IsNullOrWhiteSpace(officeAccess))
        {
            var allowedOfficeIds = officeAccess
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToHashSet();

            if (allowedOfficeIds.Count > 0 && !allowedOfficeIds.Contains(OfficeId))
                return (false, "Office access is denied.");
        }

        return (true, null);
    }
}
