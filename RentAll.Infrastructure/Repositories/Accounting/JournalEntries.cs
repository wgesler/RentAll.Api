using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            SourceTypeId = criteria.SourceTypeId,
            SourceId = criteria.SourceId,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<JournalEntry>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<JournalEntryLineSearchResult>> GetJournalEntryLinesAsync(JournalEntryLineGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryLineSearchEntity>("Accounting.JournalEntryLine_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            ChartOfAccountId = criteria.ChartOfAccountId,
            SourceTypeId = criteria.SourceTypeId,
            SourceId = criteria.SourceId,
            ReservationId = criteria.ReservationId,
            PropertyId = criteria.PropertyId,
            ContactId = criteria.ContactId,
            IncludeVoided = criteria.IncludeVoided,
            IncludeUnposted = criteria.IncludeUnposted,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<JournalEntryLineSearchResult>();

        return res.Select(ConvertLineSearchEntityToModel);
    }

    public async Task<JournalEntry?> GetJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetById", new
        {
            JournalEntryId = journalEntryId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<JournalEntry?> GetJournalEntryByCodeAsync(string journalEntryCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetByCode", new
        {
            JournalEntryCode = journalEntryCode,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsByJournalEntryCodeAsync(string journalEntryCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Accounting.JournalEntry_ExistsByCode", new
        {
            JournalEntryCode = journalEntryCode,
            OrganizationId = organizationId
        });

        return result == 1;
    }

    public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry)
    {
        return await SqlDeadlockRetry.ExecuteAsync(async () =>
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.OpenAsync();
            await using var transaction = await db.BeginTransactionAsync();

            try
            {
                var response = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_Add", new
                {
                    OrganizationId = journalEntry.OrganizationId,
                    OfficeId = journalEntry.OfficeId,
                    JournalEntryCode = journalEntry.JournalEntryCode,
                    TransactionDate = journalEntry.TransactionDate,
                    PostingDate = journalEntry.PostingDate,
                    TransactionTypeId = DefaultJournalEntryTransactionTypeId,
                    SourceTypeId = journalEntry.SourceTypeId,
                    SourceId = journalEntry.SourceId,
                    Memo = journalEntry.Memo,
                    IsPosted = journalEntry.IsPosted,
                    IsVoided = journalEntry.IsVoided,
                    CreatedBy = journalEntry.CreatedBy
                }, transaction: transaction);

                if (response == null || !response.Any())
                    throw new Exception("Journal entry not created");

                var entry = ConvertEntityToModel(response.FirstOrDefault()!);
                foreach (var line in journalEntry.JournalEntryLines)
                {
                    await db.DapperProcQueryAsync<JournalEntryLineEntity>("Accounting.JournalEntryLine_Add", new
                    {
                        JournalEntryId = entry.JournalEntryId,
                        ChartOfAccountId = line.ChartOfAccountId,
                        CostCodeId = line.CostCodeId,
                        PropertyId = line.PropertyId,
                        ReservationId = line.ReservationId,
                        ContactId = line.ContactId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Memo = line.Memo,
                        CreatedBy = journalEntry.CreatedBy
                    }, transaction: transaction);
                }

                var res = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetById", new
                {
                    JournalEntryId = entry.JournalEntryId,
                    OrganizationId = entry.OrganizationId
                }, transaction: transaction);

                if (res == null || !res.Any())
                    throw new Exception("Journal entry not found");

                await transaction.CommitAsync();
                return ConvertEntityToModel(res.FirstOrDefault()!);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<JournalEntry> UpdateJournalEntryByIdAsync(JournalEntry journalEntry)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var currentEntryResult = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetById", new
            {
                JournalEntryId = journalEntry.JournalEntryId,
                OrganizationId = journalEntry.OrganizationId
            }, transaction: transaction);

            if (currentEntryResult == null || !currentEntryResult.Any())
                throw new Exception("Journal entry not found");

            var currentEntity = currentEntryResult.FirstOrDefault()!;
            var currentEntry = ConvertEntityToModel(currentEntity);
            var currentLineIds = currentEntry.JournalEntryLines.Select(l => l.JournalEntryLineId).ToHashSet();
            var incomingActiveLines = journalEntry.JournalEntryLines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
            var incomingLineIds = incomingActiveLines.Where(l => l.JournalEntryLineId != Guid.Empty).Select(l => l.JournalEntryLineId).ToHashSet();

            var response = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_UpdateById", new
            {
                JournalEntryId = journalEntry.JournalEntryId,
                OrganizationId = journalEntry.OrganizationId,
                OfficeId = journalEntry.OfficeId,
                TransactionDate = journalEntry.TransactionDate,
                PostingDate = journalEntry.PostingDate,
                TransactionTypeId = currentEntity.TransactionTypeId,
                SourceTypeId = journalEntry.SourceTypeId,
                SourceId = journalEntry.SourceId,
                Memo = journalEntry.Memo,
                IsPosted = journalEntry.IsPosted,
                IsVoided = journalEntry.IsVoided,
                ModifiedBy = journalEntry.ModifiedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Journal entry not updated");

            var linesToDelete = currentLineIds.Except(incomingLineIds).ToList();
            foreach (var lineId in linesToDelete)
            {
                await db.DapperProcExecuteAsync("Accounting.JournalEntryLine_DeleteById", new
                {
                    JournalEntryLineId = lineId
                }, transaction: transaction);
            }

            foreach (var line in incomingActiveLines)
            {
                if (line.JournalEntryLineId == Guid.Empty)
                {
                    await db.DapperProcQueryAsync<JournalEntryLineEntity>("Accounting.JournalEntryLine_Add", new
                    {
                        JournalEntryId = journalEntry.JournalEntryId,
                        ChartOfAccountId = line.ChartOfAccountId,
                        CostCodeId = line.CostCodeId,
                        PropertyId = line.PropertyId,
                        ReservationId = line.ReservationId,
                        ContactId = line.ContactId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Memo = line.Memo,
                        CreatedBy = journalEntry.CreatedBy
                    }, transaction: transaction);
                }
                else if (currentLineIds.Contains(line.JournalEntryLineId))
                {
                    await db.DapperProcQueryAsync<JournalEntryLineEntity>("Accounting.JournalEntryLine_UpdateById", new
                    {
                        JournalEntryLineId = line.JournalEntryLineId,
                        JournalEntryId = journalEntry.JournalEntryId,
                        ChartOfAccountId = line.ChartOfAccountId,
                        CostCodeId = line.CostCodeId,
                        PropertyId = line.PropertyId,
                        ReservationId = line.ReservationId,
                        ContactId = line.ContactId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Memo = line.Memo,
                        ModifiedBy = journalEntry.ModifiedBy
                    }, transaction: transaction);
                }
            }

            var updatedEntryResult = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_GetById", new
            {
                JournalEntryId = journalEntry.JournalEntryId,
                OrganizationId = journalEntry.OrganizationId
            }, transaction: transaction);

            if (updatedEntryResult == null || !updatedEntryResult.Any())
                throw new Exception("Journal entry not updated");

            await transaction.CommitAsync();
            return ConvertEntityToModel(updatedEntryResult.FirstOrDefault()!);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.JournalEntry_DeleteById", new
        {
            JournalEntryId = journalEntryId,
            OrganizationId = organizationId
        });
    }

    public async Task<int> DeleteJournalEntriesBySourceIdAsync(Guid organizationId, Guid sourceId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryAsync<JournalEntryDeleteAllResult>("Accounting.JournalEntry_DeleteBySourceId", new
        {
            OrganizationId = organizationId,
            SourceId = sourceId,
        });

        return result?.FirstOrDefault()?.JournalEntriesDeleted ?? 0;
    }

    public async Task<int> DeleteAllJournalEntriesByOrganizationIdAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryAsync<JournalEntryDeleteAllResult>("Accounting.JournalEntry_DeleteAllByOrganizationId", new
        {
            OrganizationId = organizationId
        });

        return result?.FirstOrDefault()?.JournalEntriesDeleted ?? 0;
    }

    sealed class JournalEntryDeleteAllResult
    {
        public int JournalEntriesDeleted { get; set; }
    }
}
