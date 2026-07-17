using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    #region Get
    public async Task<IEnumerable<Transfer>> GetTransfersByCriteriaAsync(TransferGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<TransferEntity, TransferSplitEntity>("Accounting.Transfer_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = criteria.IsActive,
            IncludeInactive = criteria.IncludeInactive,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        return MapTransfersWithSplitEntities(headers, splits);
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<TransferEntity, TransferSplitEntity>("Accounting.Transfer_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        return MapTransfersWithSplitEntities(headers, splits);
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<TransferEntity, TransferSplitEntity>("Accounting.Transfer_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        return MapTransfersWithSplitEntities(headers, splits);
    }

    public async Task<Transfer?> GetTransferByIdAsync(Guid transferId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (headers, splits) = await db.DapperProcQueryMultipleAsync<TransferEntity, TransferSplitEntity>("Accounting.Transfer_GetById", new
        {
            TransferId = transferId,
            OrganizationId = organizationId
        });

        var transfers = MapTransfersWithSplitEntities(headers, splits);
        return transfers.FirstOrDefault();
    }

    #endregion

    #region Post

    public async Task<Transfer> CreateTransferAsync(Transfer transfer)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var res = await db.DapperProcQueryAsync<TransferEntity>("Accounting.Transfer_Add", new
            {
                OrganizationId = transfer.OrganizationId,
                OfficeId = transfer.OfficeId,
                TransferCode = transfer.TransferCode.Trim(),
                TransferDate = transfer.TransferDate,
                AccountingPeriod = transfer.AccountingPeriod,
                Amount = transfer.Amount,
                Description = transfer.Description,
                PropertyId = ResolveTransferHeaderPropertyId(transfer),
                BankAccountId = transfer.BankAccountId,
                Splits = SerializeTransferSplits(transfer.Splits),
                JournalEntryId = transfer.JournalEntryId,
                IsActive = transfer.IsActive,
                CreatedBy = transfer.CreatedBy
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Transfer record not created");

            var created = ConvertTransferEntityToModel(res.First());
            await InsertTransferSplitRowsAsync(db, transaction, created, transfer.Splits, transfer.CreatedBy);
            created.Splits = await GetTransferSplitsByTransferIdAsync(db, transaction, created.TransferId);
            ApplyTransferPropertyIds(created);
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

    public async Task<Transfer> UpdateTransferAsync(Transfer transfer)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updated = await UpdateTransferCoreAsync(db, transaction, transfer);
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

    public async Task DeleteTransferByIdAsync(Guid transferId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Accounting.Transfer_DeleteById", new
        {
            TransferId = transferId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }

    #endregion

    #region Helpers

    private static async Task<Transfer> UpdateTransferCoreAsync(SqlConnection db, IDbTransaction transaction, Transfer transfer)
    {
        var currentSplits = await GetTransferSplitsByTransferIdAsync(db, transaction, transfer.TransferId);

        var res = await db.DapperProcQueryAsync<TransferEntity>("Accounting.Transfer_UpdateById", new
        {
            TransferId = transfer.TransferId,
            OrganizationId = transfer.OrganizationId,
            OfficeId = transfer.OfficeId,
            TransferDate = transfer.TransferDate,
            AccountingPeriod = transfer.AccountingPeriod,
            Amount = transfer.Amount,
            Description = transfer.Description,
            PropertyId = ResolveTransferHeaderPropertyId(transfer),
            BankAccountId = transfer.BankAccountId,
            Splits = SerializeTransferSplits(transfer.Splits),
            JournalEntryId = transfer.JournalEntryId,
            HasBeenTransfered = transfer.HasBeenTransfered,
            IsActive = transfer.IsActive,
            ModifiedBy = transfer.ModifiedBy
        }, transaction: transaction);

        if (res == null || !res.Any())
            throw new Exception("Transfer record not found");

        var updated = ConvertTransferEntityToModel(res.First());
        await SyncTransferSplitRowsForUpdateAsync(
            db,
            transaction,
            updated,
            transfer.Splits ?? new List<TransferSplit>(),
            currentSplits,
            transfer.ModifiedBy);
        updated.Splits = await GetTransferSplitsByTransferIdAsync(db, transaction, updated.TransferId);
        ApplyTransferPropertyIds(updated);
        return updated;
    }

    private static List<Transfer> MapTransfersWithSplitEntities(
        IEnumerable<TransferEntity>? transferEntities,
        IEnumerable<TransferSplitEntity>? splitEntities)
    {
        if (transferEntities == null || !transferEntities.Any())
            return new List<Transfer>();

        var splitsByTransferId = (splitEntities ?? Enumerable.Empty<TransferSplitEntity>())
            .GroupBy(split => split.TransferId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertTransferSplitEntityToModel)
                    .GroupBy(split => split.TransferSplitId)
                    .Select(splitGroup => splitGroup.First())
                    .OrderBy(split => split.TransferSplitId)
                    .ToList());

        var transfers = transferEntities.Select(ConvertTransferEntityToModel).ToList();
        foreach (var transfer in transfers)
        {
            if (splitsByTransferId.TryGetValue(transfer.TransferId, out var splits) && splits.Count > 0)
                transfer.Splits = splits;

            ApplyTransferPropertyIds(transfer);
        }

        return transfers;
    }

    private static void ApplyTransferPropertyIds(Transfer transfer)
    {
        var propertyIds = new HashSet<Guid>();
        if (transfer.PropertyId.HasValue && transfer.PropertyId != Guid.Empty)
            propertyIds.Add(transfer.PropertyId.Value);

        foreach (var split in transfer.Splits ?? new List<TransferSplit>())
        {
            if (split.PropertyId.HasValue && split.PropertyId != Guid.Empty)
                propertyIds.Add(split.PropertyId.Value);
        }

        transfer.PropertyIds = propertyIds.ToList();
    }

    private static Guid? ResolveTransferHeaderPropertyId(Transfer transfer) =>
        transfer.PropertyId is { } headerPropertyId && headerPropertyId != Guid.Empty
            ? headerPropertyId
            : (transfer.Splits ?? new List<TransferSplit>())
                .Select(split => split.PropertyId)
                .FirstOrDefault(propertyId => propertyId.HasValue && propertyId != Guid.Empty);

    private static async Task<List<TransferSplit>> GetTransferSplitsByTransferIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid transferId)
    {
        var splitRows = await db.DapperProcQueryAsync<TransferSplitEntity>("Accounting.TransferSplit_GetByTransferId", new
        {
            TransferId = transferId
        }, transaction: transaction);
        if (splitRows == null || !splitRows.Any())
            return new List<TransferSplit>();

        return splitRows
            .Select(ConvertTransferSplitEntityToModel)
            .GroupBy(split => split.TransferSplitId)
            .Select(group => group.First())
            .OrderBy(split => split.TransferSplitId)
            .ToList();
    }

    private static async Task InsertTransferSplitRowsAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Transfer transfer,
        List<TransferSplit>? splitsToInsert,
        Guid auditUser)
    {
        var splits = splitsToInsert ?? new List<TransferSplit>();
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
            await db.DapperProcQueryAsync<TransferSplitEntity>("Accounting.TransferSplit_Add", new
            {
                TransferId = transfer.TransferId,
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

    private static async Task SyncTransferSplitRowsForUpdateAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Transfer transfer,
        List<TransferSplit> splitsToSync,
        List<TransferSplit> currentSplits,
        Guid auditUser)
    {
        var currentSplitIds = currentSplits.Select(split => split.TransferSplitId).ToHashSet();
        var incomingSplitIds = splitsToSync
            .Where(split => split.TransferSplitId > 0)
            .Select(split => split.TransferSplitId)
            .ToHashSet();

        var splitsToDelete = currentSplitIds.Except(incomingSplitIds).ToList();
        foreach (var transferSplitId in splitsToDelete)
        {
            await db.DapperProcExecuteAsync("Accounting.TransferSplit_DeleteById", new
            {
                TransferSplitId = transferSplitId
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

            if (split.TransferSplitId > 0 && currentSplitIds.Contains(split.TransferSplitId))
            {
                await db.DapperProcQueryAsync<TransferSplitEntity>("Accounting.TransferSplit_UpdateById", new
                {
                    TransferSplitId = split.TransferSplitId,
                    TransferId = transfer.TransferId,
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
                await db.DapperProcQueryAsync<TransferSplitEntity>("Accounting.TransferSplit_Add", new
                {
                    TransferId = transfer.TransferId,
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
