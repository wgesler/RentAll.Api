namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class GetReconcileBeginningBalanceDto
{
    public int OfficeId { get; set; }
    public int ChartOfAccountId { get; set; }
    public DateOnly? StatementDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeId <= 0)
            return (false, "Office is required");

        if (ChartOfAccountId <= 0)
            return (false, "Account is required");

        return (true, null);
    }
}
