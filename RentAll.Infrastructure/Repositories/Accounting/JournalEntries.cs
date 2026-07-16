using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class JournalEntryRepository
{
    public async Task<IEnumerable<JournalEntry>> GetJournalEntriesAsync(JournalEntryGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<JournalEntryEntity, JournalEntryLineEntity>("Accounting.JournalEntry_GetByCriteria", new
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

        return MapJournalEntriesWithLineEntities(headers, lines);
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
            UnclearedOnly = criteria.UnclearedOnly,
            IncludeCashOnly = criteria.IncludeCashOnly,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<JournalEntryLineSearchResult>();

        var results = res.Select(ConvertLineSearchEntityToModel);
        if (criteria.UnclearedOnly)
            results = results.Where(line => line.ClearedOn == null);

        return results;
    }

    public async Task<IEnumerable<JournalEntryLineSearchResult>> GetReconcileJournalEntryLinesAsync(Guid organizationId, int officeId, int chartOfAccountId, DateOnly? statementDate)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryLineSearchEntity>("Accounting.JournalEntryLine_GetForReconcile", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            ChartOfAccountId = chartOfAccountId,
            StatementDate = statementDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<JournalEntryLineSearchResult>();

        return res.Select(ConvertLineSearchEntityToModel).Where(line => line.ClearedOn == null);
    }

    public async Task<decimal> GetReconcileBeginningBalanceAsync(Guid organizationId, int officeId, int chartOfAccountId, DateOnly? statementDate)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReconcileBeginningBalanceEntity>("Accounting.JournalEntryLine_GetReconcileBeginningBalance", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            ChartOfAccountId = chartOfAccountId,
            StatementDate = statementDate
        });

        return res?.FirstOrDefault()?.BeginningBalance ?? 0m;
    }

    public async Task<JournalEntryLine?> GetJournalEntryLineByIdAsync(Guid journalEntryLineId)
    {
        if (journalEntryLineId == Guid.Empty)
            return null;

        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryLineEntity>("Accounting.JournalEntryLine_GetById", new
        {
            JournalEntryLineId = journalEntryLineId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertJournalEntryLineEntityToModel(res.First());
    }

    public async Task<JournalEntry?> GetJournalEntryByIdAsync(Guid journalEntryId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await LoadJournalEntryByIdAsync(db, null, journalEntryId, organizationId);
    }

    public async Task<JournalEntry?> GetJournalEntryByCodeAsync(string journalEntryCode, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<JournalEntryEntity, JournalEntryLineEntity>("Accounting.JournalEntry_GetByCode", new
        {
            JournalEntryCode = journalEntryCode,
            OrganizationId = organizationId
        });

        return MapJournalEntriesWithLineEntities(headers, lines).FirstOrDefault();
    }

    private async Task<JournalEntry?> LoadJournalEntryByIdAsync(SqlConnection db, IDbTransaction? transaction, Guid journalEntryId, Guid organizationId)
    {
        var (headers, lines) = await db.DapperProcQueryMultipleAsync<JournalEntryEntity, JournalEntryLineEntity>("Accounting.JournalEntry_GetById", new
        {
            JournalEntryId = journalEntryId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return MapJournalEntriesWithLineEntities(headers, lines).FirstOrDefault();
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
                    AccountingPeriod = journalEntry.AccountingPeriod,
                    PostingStatusId = (int)journalEntry.PostingStatusId,
                    TransactionTypeId = DefaultJournalEntryTransactionTypeId,
                    SourceTypeId = journalEntry.SourceTypeId,
                    SourceId = journalEntry.SourceId,
                    SourceCode = journalEntry.SourceCode,
                    Memo = journalEntry.Memo,
                    IsCashOnly = journalEntry.IsCashOnly,
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

                var reloaded = await LoadJournalEntryByIdAsync(db, transaction, entry.JournalEntryId, entry.OrganizationId);
                if (reloaded == null)
                    throw new Exception("Journal entry not found");

                await transaction.CommitAsync();
                return reloaded;
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
            var (currentHeaders, currentLines) = await db.DapperProcQueryMultipleAsync<JournalEntryEntity, JournalEntryLineEntity>("Accounting.JournalEntry_GetById", new
            {
                JournalEntryId = journalEntry.JournalEntryId,
                OrganizationId = journalEntry.OrganizationId
            }, transaction: transaction);

            var currentEntity = currentHeaders?.FirstOrDefault();
            if (currentEntity == null)
                throw new Exception("Journal entry not found");

            var currentEntry = MapJournalEntriesWithLineEntities(currentHeaders, currentLines).FirstOrDefault()
                ?? throw new Exception("Journal entry not found");

            var currentLineIds = currentEntry.JournalEntryLines.Select(l => l.JournalEntryLineId).ToHashSet();
            var incomingActiveLines = journalEntry.JournalEntryLines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
            var incomingLineIds = incomingActiveLines.Where(l => l.JournalEntryLineId != Guid.Empty).Select(l => l.JournalEntryLineId).ToHashSet();

            var response = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_UpdateById", new
            {
                JournalEntryId = journalEntry.JournalEntryId,
                OrganizationId = journalEntry.OrganizationId,
                OfficeId = journalEntry.OfficeId,
                TransactionDate = journalEntry.TransactionDate,
                AccountingPeriod = journalEntry.AccountingPeriod,
                PostingStatusId = (int)journalEntry.PostingStatusId,
                TransactionTypeId = currentEntity.TransactionTypeId,
                SourceTypeId = journalEntry.SourceTypeId,
                SourceId = journalEntry.SourceId,
                Memo = journalEntry.Memo,
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

            var updatedEntry = await LoadJournalEntryByIdAsync(db, transaction, journalEntry.JournalEntryId, journalEntry.OrganizationId);
            if (updatedEntry == null)
                throw new Exception("Journal entry not updated");

            await transaction.CommitAsync();
            return updatedEntry;
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

    public async Task<int> DeleteJournalEntriesByOfficeIdsAsync(Guid organizationId, string officeIds)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryAsync<JournalEntryDeleteAllResult>("Accounting.JournalEntry_DeleteByOfficeIds", new
        {
            OrganizationId = organizationId,
            OfficeIds = officeIds
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

    public async Task<int> DeleteOwnerStatementStartingBalancesByCriteriaAsync(Guid organizationId, Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryAsync<JournalEntryDeleteAllResult>("Accounting.OwnerStatementStartingBalance_DeleteByCriteria", new
        {
            OrganizationId = organizationId,
            PropertyId = propertyId
        });

        return result?.FirstOrDefault()?.JournalEntriesDeleted ?? 0;
    }

    sealed class JournalEntryDeleteAllResult
    {
        public int JournalEntriesDeleted { get; set; }
    }

    public async Task UpdateReconcileMarksAsync(Guid organizationId, int officeId, int chartOfAccountId, IEnumerable<ReconcileJournalEntryLineMark> lines, bool setClearedOn, DateOnly? clearedOn, Guid modifiedBy)
    {
        var lineList = lines?.ToList() ?? new List<ReconcileJournalEntryLineMark>();
        if (lineList.Count == 0)
            return;

        var linesJson = System.Text.Json.JsonSerializer.Serialize(lineList.Select(line => new
        {
            journalEntryLineId = line.JournalEntryLineId,
            isCleared = line.IsCleared
        }));

        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.JournalEntryLine_UpdateReconcileMarks", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            ChartOfAccountId = chartOfAccountId,
            LinesJson = linesJson,
            SetClearedOn = setClearedOn,
            ClearedOn = clearedOn,
            ModifiedBy = modifiedBy
        });
    }

    public async Task<JournalEntry> UpdateJournalEntryCheckNumberByIdAsync(Guid journalEntryId, Guid organizationId, string checkNumber, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<JournalEntryEntity>("Accounting.JournalEntry_UpdateCheckNumberById", new
        {
            JournalEntryId = journalEntryId,
            OrganizationId = organizationId,
            CheckNumber = checkNumber,
            ModifiedBy = modifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Journal entry not found");

        return ConvertEntityToModel(res.First());
    }
}
