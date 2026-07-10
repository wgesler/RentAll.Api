using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region BankDeposits
    public async Task<IEnumerable<Deposit>> GetDepositsByCriteriaAsync(DepositGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = criteria.IsActive,
            IncludeInactive = criteria.IncludeInactive,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Deposit>();

        var deposits = res.Select(ConvertDepositEntityToModel).ToList();
        foreach (var deposit in deposits)
            await ApplyDepositSplitsAsync(deposit);

        return deposits;
    }

    public async Task<IEnumerable<Deposit>> GetDepositsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Deposit>();

        var deposits = res.Select(ConvertDepositEntityToModel).ToList();
        foreach (var deposit in deposits)
            await ApplyDepositSplitsAsync(deposit);

        return deposits;
    }

    public async Task<IEnumerable<Deposit>> GetDepositsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Deposit>();

        var deposits = res.Select(ConvertDepositEntityToModel).ToList();
        foreach (var deposit in deposits)
            await ApplyDepositSplitsAsync(deposit);

        return deposits;
    }

    public async Task<Deposit?> GetDepositByIdAsync(Guid depositId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<DepositEntity>("Accounting.Deposit_GetById", new
        {
            DepositId = depositId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        var deposit = ConvertDepositEntityToModel(res.First());
        await ApplyDepositSplitsAsync(deposit);
        return deposit;
    }

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
                JournalEntryId = deposit.JournalEntryId,
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
            JournalEntryId = deposit.JournalEntryId,
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

    private async Task ApplyDepositSplitsAsync(Deposit deposit)
    {
        var tableSplits = await GetDepositSplitsByDepositIdAsync(deposit.DepositId);
        if (tableSplits.Count > 0)
            deposit.Splits = tableSplits;
        ApplyDepositPropertyIds(deposit);
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

    private async Task<List<DepositSplit>> GetDepositSplitsByDepositIdAsync(Guid depositId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await GetDepositSplitsByDepositIdAsync(db, null, depositId);
    }

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
            await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_Add", new
            {
                DepositId = deposit.DepositId,
                Amount = split.Amount,
                Description = split.Description,
                PropertyId = propertyId,
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

            if (split.DepositSplitId > 0 && currentSplitIds.Contains(split.DepositSplitId))
            {
                await db.DapperProcQueryAsync<DepositSplitEntity>("Accounting.DepositSplit_UpdateById", new
                {
                    DepositSplitId = split.DepositSplitId,
                    DepositId = deposit.DepositId,
                    Amount = split.Amount,
                    Description = split.Description,
                    PropertyId = propertyId,
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
                    ChartOfAccountId = chartOfAccountId,
                    CreatedBy = auditUser
                }, transaction: transaction);
            }
        }
    }
    #endregion
}
