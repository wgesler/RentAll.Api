using RentAll.Api.Dtos.Accounting.JournalEntries;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Owners;

public class OwnerPaymentResponseDto
{
    public List<JournalEntryResponseDto> JournalEntries { get; set; } = new List<JournalEntryResponseDto>();

    public OwnerPaymentResponseDto(IEnumerable<JournalEntry> journalEntries)
    {
        JournalEntries = journalEntries.Select(entry => new JournalEntryResponseDto(entry)).ToList();
    }
}
