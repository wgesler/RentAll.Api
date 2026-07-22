using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Get
    public async Task<IEnumerable<Deposit>> GetDepositsByCriteriaAsync(DepositGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<DepositEntity, DepositSplitEntity>("Accounting.Deposit_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = criteria.IsActive,
            IncludeInactive = criteria.IncludeInactive,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        return MapDepositsWithSplitEntities(headers, splits);
    }

    public async Task<IEnumerable<Deposit>> GetDepositsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<DepositEntity, DepositSplitEntity>("Accounting.Deposit_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        return MapDepositsWithSplitEntities(headers, splits);
    }

    public async Task<IEnumerable<Deposit>> GetDepositsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<DepositEntity, DepositSplitEntity>("Accounting.Deposit_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        return MapDepositsWithSplitEntities(headers, splits);
    }

    public async Task<Deposit?> GetDepositByIdAsync(Guid depositId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<DepositEntity, DepositSplitEntity>("Accounting.Deposit_GetById", new
        {
            DepositId = depositId,
            OrganizationId = organizationId
        });

        var deposits = MapDepositsWithSplitEntities(headers, splits);
        return deposits.FirstOrDefault();
    }

    #endregion

    #region Post

    public async Task<Deposit> CreateDepositAsync(Deposit deposit)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_Add", new
            {
                OrganizationId = deposit.OrganizationId,
                OfficeId = deposit.OfficeId,
                DepositCode = deposit.DepositCode.Trim(),
                DepositDate = deposit.DepositDate,
                AccountingPeriod = deposit.AccountingPeriod,
                Amount = deposit.Amount,
                Description = deposit.Description,
                PropertyId = ResolveDepositHeaderPropertyId(deposit),
                BankAccountId = deposit.BankAccountId,
                Splits = SerializeDepositSplits(deposit.Splits),
                PostingStatusId = deposit.PostingStatusId ?? 0,
                IsActive = deposit.IsActive,
                CreatedBy = deposit.CreatedBy
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Deposit record not created");

            var created = ConvertDepositEntityToModel(res.First());
            await InsertDepositSplitRowsAsync(db, transaction, created, deposit.Splits, deposit.CreatedBy);
            created.Splits = await GetDepositSplitsByDepositIdAsync(db, transaction, created.DepositId);
            ApplyDepositPropertyIds(created);
            await transaction.CommitAsync();
            return created;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Put

    public async Task<Deposit> UpdateDepositAsync(Deposit deposit)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updated = await UpdateDepositCoreAsync(db, transaction, deposit);
            await transaction.CommitAsync();
            return updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Delete

    public async Task DeleteDepositByIdAsync(Guid depositId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Deposit_DeleteById", new
        {
            DepositId = depositId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }

    #endregion

    #region Helpers

    private static async Task<Deposit> UpdateDepositCoreAsync(SqlConnection db, IDbTransaction transaction, Deposit deposit)
    {
        var currentSplits = await GetDepositSplitsByDepositIdAsync(db, transaction, deposit.DepositId);

        var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_UpdateById", new
        {
            DepositId = deposit.DepositId,
            OrganizationId = deposit.OrganizationId,
            OfficeId = deposit.OfficeId,
            DepositDate = deposit.DepositDate,
            AccountingPeriod = deposit.AccountingPeriod,
            Amount = deposit.Amount,
            Description = deposit.Description,
            PropertyId = ResolveDepositHeaderPropertyId(deposit),
            BankAccountId = deposit.BankAccountId,
            Splits = SerializeDepositSplits(deposit.Splits),
            PostingStatusId = deposit.PostingStatusId ?? 0,
            IsActive = deposit.IsActive,
            ModifiedBy = deposit.ModifiedBy
        }, transaction: transaction);

        if (res == null || !res.Any())
            throw new Exception("Deposit record not found");

        var updated = ConvertDepositEntityToModel(res.First());
        await SyncDepositSplitRowsForUpdateAsync(
            db,
            transaction,
            updated,
            deposit.Splits ?? new List<DepositSplit>(),
            currentSplits,
            deposit.ModifiedBy);
        updated.Splits = await GetDepositSplitsByDepositIdAsync(db, transaction, updated.DepositId);
        ApplyDepositPropertyIds(updated);
        return updated;
    }

    private static List<Deposit> MapDepositsWithSplitEntities(
        IEnumerable<DepositEntity>? depositEntities,
        IEnumerable<DepositSplitEntity>? splitEntities)
    {
        if (depositEntities == null || !depositEntities.Any())
            return new List<Deposit>();

        var splitsByDepositId = (splitEntities ?? Enumerable.Empty<DepositSplitEntity>())
            .GroupBy(split => split.DepositId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertDepositSplitEntityToModel)
                    .GroupBy(split => split.DepositSplitId)
                    .Select(splitGroup => splitGroup.First())
                    .OrderBy(split => split.DepositSplitId)
                    .ToList());

        var deposits = depositEntities.Select(ConvertDepositEntityToModel).ToList();
        foreach (var deposit in deposits)
        {
            if (splitsByDepositId.TryGetValue(deposit.DepositId, out var splits) && splits.Count > 0)
                deposit.Splits = splits;

            ApplyDepositPropertyIds(deposit);
        }

        return deposits;
    }

    private static void ApplyDepositPropertyIds(Deposit deposit)
    {
        var propertyIds = new HashSet<Guid>();
        if (deposit.PropertyId.HasValue && deposit.PropertyId != Guid.Empty)
            propertyIds.Add(deposit.PropertyId.Value);

        foreach (var split in deposit.Splits ?? new List<DepositSplit>())
        {
            if (split.PropertyId.HasValue && split.PropertyId != Guid.Empty)
                propertyIds.Add(split.PropertyId.Value);
        }

        deposit.PropertyIds = propertyIds.ToList();
    }

    private static Guid? ResolveDepositHeaderPropertyId(Deposit deposit) =>
        deposit.PropertyId is { } headerPropertyId && headerPropertyId != Guid.Empty
            ? headerPropertyId
            : (deposit.Splits ?? new List<DepositSplit>())
                .Select(split => split.PropertyId)
                .FirstOrDefault(propertyId => propertyId.HasValue && propertyId != Guid.Empty);

    private static async Task<List<DepositSplit>> GetDepositSplitsByDepositIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid depositId)
    {
        var splitRows = await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_GetByDepositId", new
        {
            DepositId = depositId
        }, transaction: transaction);
        if (splitRows == null || !splitRows.Any())
            return new List<DepositSplit>();

        return splitRows
            .Select(ConvertDepositSplitEntityToModel)
            .GroupBy(split => split.DepositSplitId)
            .Select(group => group.First())
            .OrderBy(split => split.DepositSplitId)
            .ToList();
    }

    private static async Task InsertDepositSplitRowsAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Deposit deposit,
        List<DepositSplit>? splitsToInsert,
        Guid auditUser)
    {
        var splits = splitsToInsert ?? new List<DepositSplit>();
        if (splits.Count == 0)
            return;

        foreach (var split in splits)
        {
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;
            Guid? propertyId = split.PropertyId is { } incomingPropertyId && incomingPropertyId != Guid.Empty
                ? incomingPropertyId
                : null;
            Guid? reservationId = split.ReservationId is { } incomingReservationId && incomingReservationId != Guid.Empty
                ? incomingReservationId
                : null;
            Guid? contactId = split.ContactId is { } incomingContactId && incomingContactId != Guid.Empty
                ? incomingContactId
                : null;
            Guid? journalEntryLineId = split.JournalEntryLineId is { } incomingJournalEntryLineId && incomingJournalEntryLineId != Guid.Empty
                ? incomingJournalEntryLineId
                : null;
            await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_Add", new
            {
                DepositId = deposit.DepositId,
                Amount = split.Amount,
                Description = split.Description,
                PropertyId = propertyId,
                ReservationId = reservationId,
                ContactId = contactId,
                JournalEntryLineId = journalEntryLineId,
                ChartOfAccountId = chartOfAccountId,
                CreatedBy = auditUser
            }, transaction: transaction);
        }
    }

    private static async Task SyncDepositSplitRowsForUpdateAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Deposit deposit,
        List<DepositSplit> splitsToSync,
        List<DepositSplit> currentSplits,
        Guid auditUser)
    {
        var currentSplitIds = currentSplits.Select(split => split.DepositSplitId).ToHashSet();
        var incomingSplitIds = splitsToSync
            .Where(split => split.DepositSplitId > 0)
            .Select(split => split.DepositSplitId)
            .ToHashSet();

        var splitsToDelete = currentSplitIds.Except(incomingSplitIds).ToList();
        foreach (var depositSplitId in splitsToDelete)
        {
            await db.DapperProcExecuteAsync("Accounting.DepositSplit_DeleteById", new
            {
                DepositSplitId = depositSplitId
            }, transaction: transaction);
        }

        if (splitsToSync.Count == 0)
            return;

        foreach (var split in splitsToSync)
        {
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;
            Guid? propertyId = split.PropertyId is { } incomingPropertyId && incomingPropertyId != Guid.Empty
                ? incomingPropertyId
                : null;
            Guid? reservationId = split.ReservationId is { } incomingReservationId && incomingReservationId != Guid.Empty
                ? incomingReservationId
                : null;
            Guid? contactId = split.ContactId is { } incomingContactId && incomingContactId != Guid.Empty
                ? incomingContactId
                : null;
            Guid? journalEntryLineId = split.JournalEntryLineId is { } incomingJournalEntryLineId && incomingJournalEntryLineId != Guid.Empty
                ? incomingJournalEntryLineId
                : null;

            if (split.DepositSplitId > 0 && currentSplitIds.Contains(split.DepositSplitId))
            {
                await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_UpdateById", new
                {
                    DepositSplitId = split.DepositSplitId,
                    DepositId = deposit.DepositId,
                    Amount = split.Amount,
                    Description = split.Description,
                    PropertyId = propertyId,
                    ReservationId = reservationId,
                    ContactId = contactId,
                    JournalEntryLineId = journalEntryLineId,
                    ChartOfAccountId = chartOfAccountId,
                    ModifiedBy = auditUser
                }, transaction: transaction);
            }
            else
            {
                await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_Add", new
                {
                    DepositId = deposit.DepositId,
                    Amount = split.Amount,
                    Description = split.Description,
                    PropertyId = propertyId,
                    ReservationId = reservationId,
                    ContactId = contactId,
                    JournalEntryLineId = journalEntryLineId,
                    ChartOfAccountId = chartOfAccountId,
                    CreatedBy = auditUser
                }, transaction: transaction);
            }
        }
    }
    #endregion
}
