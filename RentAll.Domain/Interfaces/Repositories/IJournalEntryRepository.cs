using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IJournalEntryRepository
{
    Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryGetCriteria criteria);
    Task<IEnumerable<JournalEntryLineSearchResult>> GetJournalEntryLinesAsync(JournalEntryLineGetCriteria criteria);
    Task<decimal> GetReconcileBeginningBalanceAsync(Guid organizationId, int officeId, int chartOfAccountId, DateOnly? statementDate);
    Task<JournalEntryLine?> GetJournalEntryLineByIdAsync(Guid journalEntryLineId);
    Task<IEnumerable<JournalEntryRecapLine>> GetJournalEntryRecapLinesAsync(JournalEntryRecapGetCriteria criteria);
    Task<JournalEntry?> GetJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry?> GetJournalEntryByCodeAsync(string journalEntryCode, Guid organizationId);
    Task<bool> ExistsByJournalEntryCodeAsync(string journalEntryCode, Guid organizationId);
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryByIdAsync(JournalEntry journalEntry);
    Task DeleteJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task<int> DeleteJournalEntriesBySourceIdAsync(Guid organizationId, Guid sourceId);
    Task<int> DeleteJournalEntriesByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<int> DeleteAllJournalEntriesByOrganizationIdAsync(Guid organizationId);
    Task<int> DeleteOwnerStatementStartingBalancesByCriteriaAsync(Guid organizationId, Guid propertyId);
}
