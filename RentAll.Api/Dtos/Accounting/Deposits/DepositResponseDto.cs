using RentAll.Api.Dtos.Accounting.JournalEntries;

namespace RentAll.Api.Dtos.Accounting.Deposits;

public class DepositResponseDto
{
    public JournalEntryResponseDto JournalEntry { get; set; }

    public DepositResponseDto(JournalEntry journalEntry)
    {
        JournalEntry = new JournalEntryResponseDto(journalEntry);
    }
}
