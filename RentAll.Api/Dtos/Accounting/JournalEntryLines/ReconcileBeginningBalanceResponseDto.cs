namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

public class ReconcileBeginningBalanceResponseDto
{
    public decimal BeginningBalance { get; set; }

    public ReconcileBeginningBalanceResponseDto(decimal beginningBalance)
    {
        BeginningBalance = beginningBalance;
    }
}
