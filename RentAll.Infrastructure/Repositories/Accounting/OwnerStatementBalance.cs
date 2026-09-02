using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<IReadOnlyList<OwnerStatementPropertyLedgerBalance>> GetOwnerStatementPropertyLedgersAsync(
        Guid organizationId,
        string officeIds,
        DateOnly asOfDate,
        Guid? propertyId = null,
        Guid? excludeJournalEntryId = null,
        bool includeUnposted = true)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var entities = await db.DapperProcQueryAsync<OwnerStatementPropertyLedgerBalanceEntity>(
            "Accounting.OwnerStatementBalance_GetPropertyLedgers",
            new
            {
                OrganizationId = organizationId,
                OfficeIds = officeIds,
                PropertyId = propertyId,
                AsOfDate = asOfDate.ToDateTime(TimeOnly.MinValue),
                ExcludeJournalEntryId = excludeJournalEntryId,
                IncludeUnposted = includeUnposted
            });

        return (entities ?? Enumerable.Empty<OwnerStatementPropertyLedgerBalanceEntity>())
            .Select(ConvertOwnerStatementPropertyLedgerBalanceEntityToModel)
            .ToList();
    }

    public async Task<Guid?> FindOwnerBalanceJournalEntryIdByMemoAsync(
        Guid organizationId,
        int officeId,
        Guid propertyId,
        string memo)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var journalEntryId = await db.DapperProcQueryScalarAsync<Guid?>(
            "Accounting.OwnerStatementBalance_FindJournalEntryByMemo",
            new
            {
                OrganizationId = organizationId,
                OfficeId = officeId,
                PropertyId = propertyId,
                Memo = memo
            });

        return journalEntryId == Guid.Empty ? null : journalEntryId;
    }

    private static OwnerStatementPropertyLedgerBalance ConvertOwnerStatementPropertyLedgerBalanceEntityToModel(
        OwnerStatementPropertyLedgerBalanceEntity entity)
        => new()
        {
            OfficeId = entity.OfficeId,
            PropertyId = entity.PropertyId,
            LedgerBalance = entity.LedgerBalance
        };
}
