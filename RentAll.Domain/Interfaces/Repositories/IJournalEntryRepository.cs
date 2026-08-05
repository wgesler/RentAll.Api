using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IJournalEntryRepository
{
    Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryGetCriteria criteria);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesBySourceIdAsync(JournalEntryGetBySourceIdCriteria criteria);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByPaymentIdAsync(JournalEntryGetByPaymentIdCriteria criteria);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByDepositIdAsync(JournalEntryGetByDepositIdCriteria criteria);
    Task<IEnumerable<JournalEntry>> GetJournalEntriesByTransferIdAsync(JournalEntryGetByTransferIdCriteria criteria);
    Task<IEnumerable<JournalEntryLineSearchResult>> GetJournalEntryLinesAsync(JournalEntryLineGetCriteria criteria);
    Task<IEnumerable<JournalEntryLineSearchResult>> GetOwnerApAgingJournalEntryLinesAsync(JournalEntryLineOwnerApAgingGetCriteria criteria);
    Task<IEnumerable<JournalEntryLineSearchResult>> GetReconcileJournalEntryLinesAsync(Guid organizationId, int officeId, int chartOfAccountId, DateOnly? statementDate);
    Task<decimal> GetReconcileBeginningBalanceAsync(Guid organizationId, int officeId, int chartOfAccountId, DateOnly? statementDate);
    Task<JournalEntryLine?> GetJournalEntryLineByIdAsync(Guid journalEntryLineId);
    Task<IEnumerable<JournalEntryRecapLine>> GetJournalEntryRecapLinesAsync(JournalEntryRecapGetCriteria criteria);
    Task<OwnerReportBundleData> GetOwnerReportBundleDataAsync(JournalEntryRecapGetCriteria criteria, DateOnly? priorMonthCloseDate, DateOnly? periodStartDate);
    Task<EscrowReportBundleData> GetEscrowReportBundleDataAsync(JournalEntryRecapGetCriteria criteria, bool includeDrillDownLines = false);
    Task<JournalEntry?> GetJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task<JournalEntry?> GetJournalEntryByCodeAsync(string journalEntryCode, Guid organizationId);
    Task<bool> ExistsByJournalEntryCodeAsync(string journalEntryCode, Guid organizationId);
    Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryByIdAsync(JournalEntry journalEntry);
    Task<JournalEntry> UpdateJournalEntryCheckNumberByIdAsync(Guid journalEntryId, Guid organizationId, string checkNumber, Guid modifiedBy);
    Task DeleteJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task DeleteOpenJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId);
    Task<int> DeleteJournalEntriesBySourceIdAsync(Guid organizationId, int sourceTypeId, Guid sourceId, int? journalEntryKindId = null, bool includeCashOnly = true);
    Task<int> DeleteOpenJournalEntriesBySourceCodesAsync(Guid organizationId, int officeId, IEnumerable<int> sourceTypeIds, IEnumerable<string> sourceCodes);
    Task<int> DeleteJournalEntriesByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<int> DeleteAllJournalEntriesByOrganizationIdAsync(Guid organizationId);
    Task UpdateReconcileMarksAsync(Guid organizationId, int officeId, int chartOfAccountId, IEnumerable<ReconcileJournalEntryLineMark> lines, bool setClearedOn, DateOnly? clearedOn, Guid modifiedBy);
}
