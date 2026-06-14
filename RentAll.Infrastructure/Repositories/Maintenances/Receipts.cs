using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

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
            receipt.Splits = await GetReceiptSplitsByReceiptIdAsync(receipt.ReceiptId);

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
            receipt.Splits = await GetReceiptSplitsByReceiptIdAsync(receipt.ReceiptId);

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
            receipt.Splits = await GetReceiptSplitsByReceiptIdAsync(receipt.ReceiptId);

        return receipts;
    }

    public async Task<Receipt?> GetReceiptByIdAsync(int receiptId, Guid organizationId)
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
        receipt.Splits = await GetReceiptSplitsByReceiptIdAsync(receipt.ReceiptId);
        return receipt;
    }
    #endregion

    #region Creates
    public async Task<Receipt> CreateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
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
            IsActive = receipt.IsActive,
            CreatedBy = receipt.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Receipt record not created");

        var created = ConvertEntityToModel(res.First());
        await SyncReceiptSplitRowsAsync(created, receipt.Splits, receipt.CreatedBy);
        created.Splits = await GetReceiptSplitsByReceiptIdAsync(created.ReceiptId);
        return created;
    }
    #endregion

    #region Updates
    public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
    {
        await using var db = new SqlConnection(_dbConnectionString);
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
            IsActive = receipt.IsActive,
            ModifiedBy = receipt.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Receipt record not found");

        var updated = ConvertEntityToModel(res.First());
        await SyncReceiptSplitRowsAsync(updated, receipt.Splits, receipt.ModifiedBy);
        updated.Splits = await GetReceiptSplitsByReceiptIdAsync(updated.ReceiptId);
        return updated;
    }
    #endregion

    #region Deletes
    public async Task DeleteReceiptByIdAsync(int receiptId, Guid organizationId, Guid currentUser)
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
    private async Task<List<ReceiptSplit>> GetReceiptSplitsByReceiptIdAsync(int receiptId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var splitRows = await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_GetByReceiptId", new
        {
            ReceiptId = receiptId
        });
        if (splitRows == null || !splitRows.Any())
            return new List<ReceiptSplit>();

        return splitRows
            .Select(ConvertEntityToModel)
            .GroupBy(split => split.ReceiptSplitId)
            .Select(group => group.First())
            .OrderBy(split => split.ReceiptSplitId)
            .ToList();
    }

    private async Task SyncReceiptSplitRowsAsync(Receipt receipt, List<ReceiptSplit>? splitsToSync, Guid auditUser)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.ReceiptSplit_DeleteByReceiptId", new
        {
            ReceiptId = receipt.ReceiptId
        });

        var splits = splitsToSync ?? new List<ReceiptSplit>();
        if (splits.Count == 0)
            return;

        Dictionary<string, Guid> workOrderCodeLookup = new(StringComparer.OrdinalIgnoreCase);
        if (splits.Any(split => !split.WorkOrderId.HasValue && !string.IsNullOrWhiteSpace(split.WorkOrder)))
        {
            var workOrders = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByOfficeIds", new
            {
                OrganizationId = receipt.OrganizationId,
                Offices = receipt.OfficeId.ToString()
            });
            workOrderCodeLookup = (workOrders ?? Enumerable.Empty<WorkOrderEntity>())
                .Where(workOrder => workOrder.WorkOrderId != Guid.Empty && !string.IsNullOrWhiteSpace(workOrder.WorkOrderCode))
                .GroupBy(workOrder => workOrder.WorkOrderCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().WorkOrderId, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var split in splits)
        {
            var workOrderId = split.WorkOrderId;
            if (!workOrderId.HasValue && !string.IsNullOrWhiteSpace(split.WorkOrder))
            {
                var code = split.WorkOrder.Trim();
                if (workOrderCodeLookup.TryGetValue(code, out var resolvedWorkOrderId))
                    workOrderId = resolvedWorkOrderId;
            }

            await db.DapperProcQueryAsync<ReceiptSplitEntity>("Maintenance.ReceiptSplit_Add", new
            {
                ReceiptId = receipt.ReceiptId,
                Amount = split.Amount,
                Description = split.Description,
                ReceiptTypeId = split.ReceiptTypeId,
                WorkOrderId = workOrderId,
                ChartOfAccountId = split.ChartOfAccountId is > 0 ? split.ChartOfAccountId : null,
                CreatedBy = auditUser
            });
        }
    }
    #endregion
}
