using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IReportManager
{
    Task<RecapReport> GetJournalEntryRecapReportAsync(JournalEntryRecapGetCriteria criteria);
    Task<OwnerCashReport> GetOwnerCashReportAsync(JournalEntryRecapGetCriteria criteria);
}
