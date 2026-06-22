using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Receipt>> GetReceiptsByCriteriaAsync(ReceiptGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IncludeInactive = criteria.IncludeInactive,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate,
            ReceiptKind = criteria.ReceiptKind.HasValue ? (byte?)criteria.ReceiptKind.Value : null
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Receipt>();

        var receipts = res.Select(ConvertEntityToModel).ToList();
        foreach (var receipt in receipts)
            await ApplyReceiptSplitsAsync(receipt);

        return receipts;
    }

    public async Task<IEnumerable<Receipt>> GetReceiptsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Receipt>();

        var receipts = res.Select(ConvertEntityToModel).ToList();
        foreach (var receipt in receipts)
            await ApplyReceiptSplitsAsync(receipt);

        return receipts;
    }

    public async Task<IEnumerable<Receipt>> GetReceiptsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Receipt>();

        var receipts = res.Select(ConvertEntityToModel).ToList();
        foreach (var receipt in receipts)
            await ApplyReceiptSplitsAsync(receipt);

        return receipts;
    }

    public async Task<Receipt?> GetReceiptByIdAsync(Guid receiptId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_GetById", new
        {
            ReceiptId = receiptId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        var receipt = ConvertEntityToModel(res.First());
        await ApplyReceiptSplitsAsync(receipt);
        return receipt;
    }
    #endregion

    #region Creates
    public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_Add", new
            {
                OrganizationId = receipt.OrganizationId,
                OfficeId = receipt.OfficeId,
                ReceiptCode = receipt.ReceiptCode.Trim(),
                Properties = SerializeReceiptPropertyIds(receipt.PropertyIds),
                ReceiptDate = receipt.ReceiptDate,
                DueDate = receipt.DueDate,
                AccountingPeriod = receipt.AccountingPeriod,
                BillNumber = receipt.BillNumber,
                Amount = receipt.Amount,
                PaidAmount = receipt.PaidAmount,
                PaidDate = receipt.PaidDate,
                Description = receipt.Description,
                BankCardId = receipt.BankCardId,
                VendorId = receipt.VendorId,
                VendorName = receipt.VendorName,
                Splits = SerializeReceiptSplits(receipt.Splits),
                ReceiptPath = receipt.ReceiptPath,
                PaymentTypeId = receipt.PaymentTypeId,
                CheckPrinted = receipt.CheckPrinted,
                IsUtility = receipt.IsUtility,
                IsActive = receipt.IsActive,
                CreatedBy = receipt.CreatedBy
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Receipt record not created");

            var created = ConvertEntityToModel(res.First());
            await InsertReceiptSplitRowsAsync(db, transaction, created, receipt.Splits, receipt.CreatedBy);
            created.Splits = await GetReceiptSplitsByReceiptIdAsync(db, transaction, created.ReceiptId);
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

    #region Updates
    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updated = await UpdateReceiptCoreAsync(db, transaction, receipt);
            await transaction.CommitAsync();
            return updated;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<Receipt>> UpdateReceiptsInTransactionAsync(IReadOnlyList<Receipt> receipts)
    {
        if (receipts.Count == 0)
            return receipts;

        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var updatedReceipts = new List<Receipt>(receipts.Count);
            foreach (var receipt in receipts)
                updatedReceipts.Add(await UpdateReceiptCoreAsync(db, transaction, receipt));

            await transaction.CommitAsync();
            return updatedReceipts;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<Receipt> UpdateReceiptCoreAsync(SqlConnection db, IDbTransaction transaction, Receipt receipt)
    {
        var currentSplits = await GetReceiptSplitsByReceiptIdAsync(db, transaction, receipt.ReceiptId);

        var res = await db.DapperProcQueryAsync<ReceiptEntity>("Maintenance.Receipt_UpdateById", new
        {
            ReceiptId = receipt.ReceiptId,
            OrganizationId = receipt.OrganizationId,
            OfficeId = receipt.OfficeId,
            Properties = SerializeReceiptPropertyIds(receipt.PropertyIds),
            ReceiptDate = receipt.ReceiptDate,
            DueDate = receipt.DueDate,
            AccountingPeriod = receipt.AccountingPeriod,
            BillNumber = receipt.BillNumber,
            Amount = receipt.Amount,
            PaidAmount = receipt.PaidAmount,
            PaidDate = receipt.PaidDate,
            Description = receipt.Description,
            BankCardId = receipt.BankCardId,
            VendorId = receipt.VendorId,
            VendorName = receipt.VendorName,
            Splits = SerializeReceiptSplits(receipt.Splits),
            ReceiptPath = receipt.ReceiptPath,
            PaymentTypeId = receipt.PaymentTypeId,
            CheckPrinted = receipt.CheckPrinted,
            IsUtility = receipt.IsUtility,
            IsActive = receipt.IsActive,
            ModifiedBy = receipt.ModifiedBy
        }, transaction: transaction);

        if (res == null || !res.Any())
            throw new Exception("Receipt record not found");

        var updated = ConvertEntityToModel(res.First());
        await SyncReceiptSplitRowsForUpdateAsync(
            db,
            transaction,
            updated,
            receipt.Splits ?? new List<ReceiptSplit>(),
            currentSplits,
            receipt.ModifiedBy);
        updated.Splits = await GetReceiptSplitsByReceiptIdAsync(db, transaction, updated.ReceiptId);
        return updated;
    }
    #endregion

    #region Deletes
    public async Task DeleteReceiptByIdAsync(Guid receiptId, Guid organizationId, Guid currentUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Receipt_DeleteById", new
        {
            ReceiptId = receiptId,
            OrganizationId = organizationId,
            ModifiedBy = currentUser
        });
    }
    #endregion

    #region Private Methods
    private async Task ApplyReceiptSplitsAsync(Receipt receipt)
    {
        var tableSplits = await GetReceiptSplitsByReceiptIdAsync(receipt.ReceiptId);
        if (tableSplits.Count > 0)
            receipt.Splits = tableSplits;
    }

    private async Task<List<ReceiptSplit>> GetReceiptSplitsByReceiptIdAsync(Guid receiptId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await GetReceiptSplitsByReceiptIdAsync(db, null, receiptId);
    }

    private static async Task<List<ReceiptSplit>> GetReceiptSplitsByReceiptIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid receiptId)
    {
        var splitRows = await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_GetByReceiptId", new
        {
            ReceiptId = receiptId
        }, transaction: transaction);
        if (splitRows == null || !splitRows.Any())
            return new List<ReceiptSplit>();

        return splitRows
            .Select(ConvertEntityToModel)
            .GroupBy(split => split.ReceiptSplitId)
            .Select(group => group.First())
            .OrderBy(split => split.ReceiptSplitId)
            .ToList();
    }

    private static async Task InsertReceiptSplitRowsAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Receipt receipt,
        List<ReceiptSplit>? splitsToInsert,
        Guid auditUser)
    {
        var splits = splitsToInsert ?? new List<ReceiptSplit>();
        if (splits.Count == 0)
            return;

        var workOrderCodeLookup = await BuildWorkOrderCodeLookupAsync(db, transaction, receipt, splits);
        foreach (var split in splits)
        {
            var workOrderId = ResolveSplitWorkOrderId(split, existing: null, workOrderCodeLookup);
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;
            await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_Add", new
            {
                ReceiptId = receipt.ReceiptId,
                Amount = split.Amount,
                Description = split.Description,
                ReceiptTypeId = split.ReceiptTypeId,
                WorkOrderId = workOrderId,
                ChartOfAccountId = chartOfAccountId is > 0 ? chartOfAccountId : null,
                CreatedBy = auditUser
            }, transaction: transaction);
        }
    }

    private static async Task SyncReceiptSplitRowsForUpdateAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Receipt receipt,
        List<ReceiptSplit> splitsToSync,
        List<ReceiptSplit> currentSplits,
        Guid auditUser)
    {
        var currentSplitIds = currentSplits.Select(split => split.ReceiptSplitId).ToHashSet();
        var incomingSplitIds = splitsToSync
            .Where(split => split.ReceiptSplitId > 0)
            .Select(split => split.ReceiptSplitId)
            .ToHashSet();

        var splitsToDelete = currentSplitIds.Except(incomingSplitIds).ToList();
        foreach (var receiptSplitId in splitsToDelete)
        {
            await db.DapperProcExecuteAsync("Maintenance.ReceiptSplit_DeleteById", new
            {
                ReceiptSplitId = receiptSplitId
            }, transaction: transaction);
        }

        if (splitsToSync.Count == 0)
            return;

        var workOrderCodeLookup = await BuildWorkOrderCodeLookupAsync(db, transaction, receipt, splitsToSync);
        var currentById = currentSplits.ToDictionary(split => split.ReceiptSplitId);

        foreach (var split in splitsToSync)
        {
            var existing = split.ReceiptSplitId > 0 && currentById.TryGetValue(split.ReceiptSplitId, out var match)
                ? match
                : null;
            var workOrderId = ResolveSplitWorkOrderId(split, existing, workOrderCodeLookup);
            var chartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null;

            if (split.ReceiptSplitId > 0 && currentSplitIds.Contains(split.ReceiptSplitId))
            {
                await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_UpdateById", new
                {
                    ReceiptSplitId = split.ReceiptSplitId,
                    ReceiptId = receipt.ReceiptId,
                    Amount = split.Amount,
                    Description = split.Description,
                    ReceiptTypeId = split.ReceiptTypeId,
                    WorkOrderId = workOrderId,
                    ChartOfAccountId = chartOfAccountId,
                    ModifiedBy = auditUser
                }, transaction: transaction);
            }
            else
            {
                await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_Add", new
                {
                    ReceiptId = receipt.ReceiptId,
                    Amount = split.Amount,
                    Description = split.Description,
                    ReceiptTypeId = split.ReceiptTypeId,
                    WorkOrderId = workOrderId,
                    ChartOfAccountId = chartOfAccountId,
                    CreatedBy = auditUser
                }, transaction: transaction);
            }
        }
    }

    private static async Task<Dictionary<string, Guid>> BuildWorkOrderCodeLookupAsync(
        SqlConnection db,
        IDbTransaction transaction,
        Receipt receipt,
        IEnumerable<ReceiptSplit> splits)
    {
        if (!splits.Any(split => NeedsWorkOrderCodeLookup(split)))
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var workOrders = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByOfficeIds", new
        {
            OrganizationId = receipt.OrganizationId,
            Offices = receipt.OfficeId.ToString()
        }, transaction: transaction);

        return (workOrders ?? Enumerable.Empty<WorkOrderEntity>())
            .Where(workOrder => workOrder.WorkOrderId != Guid.Empty && !string.IsNullOrWhiteSpace(workOrder.WorkOrderCode))
            .GroupBy(workOrder => workOrder.WorkOrderCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().WorkOrderId, StringComparer.OrdinalIgnoreCase);
    }

    private static bool NeedsWorkOrderCodeLookup(ReceiptSplit split) =>
        !split.WorkOrderId.HasValue
        && (!string.IsNullOrWhiteSpace(split.WorkOrderCode) || !string.IsNullOrWhiteSpace(split.WorkOrder));

    private static bool HasWorkOrderFieldPresence(ReceiptSplit split) =>
        split.WorkOrderId.HasValue
        || split.WorkOrderCode is not null
        || split.WorkOrder is not null;

    private static Guid? ResolveSplitWorkOrderId(
        ReceiptSplit incoming,
        ReceiptSplit? existing,
        Dictionary<string, Guid> workOrderCodeLookup)
    {
        if (incoming.WorkOrderId.HasValue)
            return incoming.WorkOrderId;

        var codeToResolve = !string.IsNullOrWhiteSpace(incoming.WorkOrderCode)
            ? incoming.WorkOrderCode.Trim()
            : incoming.WorkOrder?.Trim();

        if (!string.IsNullOrWhiteSpace(codeToResolve)
            && workOrderCodeLookup.TryGetValue(codeToResolve, out var resolvedWorkOrderId))
            return resolvedWorkOrderId;

        if (HasWorkOrderFieldPresence(incoming))
            return null;

        return existing?.WorkOrderId;
    }
    #endregion
}
