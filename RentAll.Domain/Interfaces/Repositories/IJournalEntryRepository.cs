using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IJournalEntryRepository
{
    Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryGetCriteria criteria);
    Task<JournalEntry?> GetJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryByIdAsync(JournalEntry journalEntry);
    Task DeleteJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
}
