using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Repositories.Reservations;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Selects
    public async Task<SecurityDepositListData> GetUnreturnedSecurityDepositDataAsync(Guid organizationId, string officeAccess, bool includeCompleted = false)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (rows, invoices, ledgerLines, payments, journalEntries, journalEntryLines) = await db.DapperProcQuerySextupleAsync<
            ReservationDepartureEntity,
            InvoiceEntity,
            LedgerLineEntity,
            PaymentEntity,
            JournalEntryEntity,
            JournalEntryLineEntity>("Property.Reservation_GetUnreturnedSecurityDeposits", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess,
                IncludeCompleted = includeCompleted
            });

        return new SecurityDepositListData
        {
            Rows = rows.Select(ReservationRepository.ConvertDepartureEntityToModel).ToList(),
            Invoices = MapInvoicesWithLedgerLineEntities(invoices, ledgerLines),
            Payments = MapPaymentsWithLedgerLineEntities(payments, null),
            JournalEntries = JournalEntryRepository.MapJournalEntriesWithLineEntities(journalEntries, journalEntryLines, _logger)
        };
    }
    #endregion
}
