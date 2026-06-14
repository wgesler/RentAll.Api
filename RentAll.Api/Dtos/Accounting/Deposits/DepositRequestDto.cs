namespace RentAll.Api.Dtos.Accounting.Deposits;

public class DepositRequestDto
{
    public int OfficeId { get; set; }
    public DateOnly DepositDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public List<Guid> JournalEntryLineIds { get; set; } = new List<Guid>();

    public (bool IsValid, string? ErrorMessage) IsValid(string officeAccess)
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!officeAccess
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(id => int.TryParse(id, out var parsedOfficeId) && parsedOfficeId == OfficeId))
            return (false, "Invalid office");

        if (DepositDate == default)
            return (false, "DepositDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (Amount == 0)
            return (false, "No deposit amount submitted");

        if (JournalEntryLineIds.Count <= 0)
            return (false, "No journal entry lines submitted for deposit");

        if (JournalEntryLineIds.Any(lineId => lineId == Guid.Empty))
            return (false, "Invalid journal entry line id submitted for deposit");

        return (true, null);
    }
}
