namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class GetReconcileJournalEntryLinesDto
{
    public int OfficeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public DateOnly? StatementDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OfficeId <= 0)
            return (false, "Office is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (ChartOfAccountId <= 0)
            return (false, "Account is required");

        return (true, null);
    }
}
