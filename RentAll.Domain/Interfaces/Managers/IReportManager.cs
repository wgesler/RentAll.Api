using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IReportManager
{
    Task<RecapReport> GetJournalEntryRecapReportAsync(JournalEntryRecapGetCriteria criteria);
    Task<TransferReport> GetTransferReportAsync(JournalEntryRecapGetCriteria criteria);
    Task<OwnerCashReport> GetOwnerCashReportAsync(JournalEntryRecapGetCriteria criteria);
    Task<OwnerAccrualReport> GetOwnerAccrualReportAsync(JournalEntryRecapGetCriteria criteria);
    Task<OwnerReportsBundle> GetOwnerReportsBundleAsync(JournalEntryRecapGetCriteria criteria);
    Task<EscrowReport> GetEscrowReportAsync(JournalEntryRecapGetCriteria criteria, decimal cushion);
    Task<IEnumerable<OwnerStatementJournalEntryLine>> GetOwnerReportJournalEntryLinesAsync(OwnerReportJournalEntryDrillDownCriteria criteria);
    Task<IEnumerable<OwnerStatementJournalEntryLine>> GetEscrowReportJournalEntryLinesAsync(EscrowReportJournalEntryDrillDownCriteria criteria);
}
