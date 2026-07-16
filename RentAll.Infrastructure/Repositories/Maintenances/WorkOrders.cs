using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Maintenances;
using System.Data;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersByCriteriaAsync(WorkOrderGetCriteria criteria)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetByCriteria", new
        {
            OrganizationId = criteria.OrganizationId,
            OfficeIds = criteria.OfficeIds,
            PropertyId = criteria.PropertyId,
            IsActive = criteria.IsActive,
            StartDate = criteria.StartDate,
            EndDate = criteria.EndDate
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<WorkOrder>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<WorkOrder>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<WorkOrder>> GetWorkOrdersByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<WorkOrder>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<WorkOrder?> GetWorkOrderByIdAsync(Guid workOrderId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        return await LoadWorkOrderByIdAsync(db, null, workOrderId, organizationId);
    }

    private async Task<WorkOrder?> LoadWorkOrderByIdAsync(
        SqlConnection db,
        IDbTransaction? transaction,
        Guid workOrderId,
        Guid organizationId)
    {
        var (headers, items) = await db.DapperProcQueryMultipleAsync<WorkOrderEntity, WorkOrderItemEntity>("Maintenance.WorkOrder_GetById", new
        {
            WorkOrderId = workOrderId,
            OrganizationId = organizationId
        }, transaction: transaction);

        return MapWorkOrdersWithItemEntities(headers, items).FirstOrDefault();
    }

    private static List<WorkOrder> MapWorkOrdersWithItemEntities(
        IEnumerable<WorkOrderEntity>? workOrderEntities,
        IEnumerable<WorkOrderItemEntity>? itemEntities)
    {
        if (workOrderEntities == null || !workOrderEntities.Any())
            return new List<WorkOrder>();

        var itemsByWorkOrderId = (itemEntities ?? Enumerable.Empty<WorkOrderItemEntity>())
            .GroupBy(item => item.WorkOrderId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(ConvertWorkOrderItemEntityToModel)
                    .GroupBy(item => item.WorkOrderItemId)
                    .Select(itemGroup => itemGroup.First())
                    .OrderBy(item => item.WorkOrderItemId)
                    .ToList());

        var workOrders = workOrderEntities.Select(ConvertEntityToModel).ToList();
        foreach (var workOrder in workOrders)
        {
            if (itemsByWorkOrderId.TryGetValue(workOrder.WorkOrderId, out var items) && items.Count > 0)
                workOrder.WorkOrderItems = items;
        }

        return workOrders;
    }
    #endregion

    #region Creates
    public async Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var response = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_Add", new
            {
                OrganizationId = workOrder.OrganizationId,
                OfficeId = workOrder.OfficeId,
                PropertyId = workOrder.PropertyId,
                ReservationId = workOrder.ReservationId,
                ReservationCode = workOrder.ReservationCode,
                WorkOrderCode = workOrder.WorkOrderCode,
                Title = workOrder.Title,
                Description = workOrder.Description,
                WorkOrderTypeId = (int)workOrder.WorkOrderType,
                ApplyMarkup = workOrder.ApplyMarkup,
                WorkOrderDate = workOrder.WorkOrderDate,
                AccountingPeriod = workOrder.AccountingPeriod == default
                    ? new DateOnly(workOrder.WorkOrderDate.Year, workOrder.WorkOrderDate.Month, 1)
                    : workOrder.AccountingPeriod,
                JournalEntryId = workOrder.JournalEntryId,
                UseDepartureFee = workOrder.UseDepartureFee,
                EnteredInQb = workOrder.EnteredInQb,
                IsActive = workOrder.IsActive,
                CreatedBy = workOrder.CreatedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Work Order not created");

            var o = ConvertEntityToModel(response.FirstOrDefault()!);

            foreach (var item in workOrder.WorkOrderItems)
            {
                var woi = await db.DapperProcQueryAsync<WorkOrderItemEntity>("Maintenance.WorkOrderItem_Add", new
                {
                    WorkOrderId = o.WorkOrderId,
                    Description = item.Description,
                    ReceiptId = item.ReceiptId,
                    LaborHours = item.LaborHours,
                    LaborCost = item.LaborCost,
                    ItemAmount = item.ItemAmount,
                    CreatedBy = workOrder.CreatedBy
                }, transaction: transaction);
            }

            var reloaded = await LoadWorkOrderByIdAsync(db, transaction, o.WorkOrderId, o.OrganizationId);
            if (reloaded == null)
                throw new Exception("Work Order not found");

            await transaction.CommitAsync();
            return reloaded;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    #endregion

    #region Updates
    public async Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var existing = await LoadWorkOrderByIdAsync(db, transaction, workOrder.WorkOrderId, workOrder.OrganizationId);
            if (existing == null)
                throw new Exception("Work Order not found");

            var currentOrder = existing;
            var currentOrderItemsIds = currentOrder.WorkOrderItems.Select(wo => wo.WorkOrderItemId).ToHashSet();
            var incomingOrderItemIds = workOrder.WorkOrderItems.Where(wo => wo.WorkOrderItemId != Guid.Empty).Select(wo => wo.WorkOrderItemId).ToHashSet();

            var response = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_UpdateById", new
            {
                WorkOrderId = workOrder.WorkOrderId,
                OrganizationId = workOrder.OrganizationId,
                OfficeId = workOrder.OfficeId,
                PropertyId = workOrder.PropertyId,
                ReservationId = workOrder.ReservationId,
                ReservationCode = workOrder.ReservationCode,
                WorkOrderCode = workOrder.WorkOrderCode,
                Title = workOrder.Title,
                Description = workOrder.Description,
                WorkOrderTypeId = (int)workOrder.WorkOrderType,
                ApplyMarkup = workOrder.ApplyMarkup,
                WorkOrderDate = workOrder.WorkOrderDate,
                AccountingPeriod = workOrder.AccountingPeriod == default
                    ? new DateOnly(workOrder.WorkOrderDate.Year, workOrder.WorkOrderDate.Month, 1)
                    : workOrder.AccountingPeriod,
                JournalEntryId = workOrder.JournalEntryId,
                UseDepartureFee = workOrder.UseDepartureFee,
                EnteredInQb = workOrder.EnteredInQb,
                IsActive = workOrder.IsActive,
                ModifiedBy = workOrder.ModifiedBy
            }, transaction: transaction);

            if (response == null || !response.Any())
                throw new Exception("Work Order not updated");

            // Sync WorkOrderItems
            foreach (var item in workOrder.WorkOrderItems)
            {
                if (item.WorkOrderItemId == Guid.Empty)
                {
                    // Create new WorkOrderItem
                    await db.DapperProcQueryAsync<WorkOrderItemEntity>("Maintenance.WorkOrderItem_Add", new
                    {
                        WorkOrderId = workOrder.WorkOrderId,
                        Description = item.Description,
                        ReceiptId = item.ReceiptId,
                        LaborHours = item.LaborHours,
                        LaborCost = item.LaborCost,
                        ItemAmount = item.ItemAmount,
                        CreatedBy = workOrder.CreatedBy
                    }, transaction: transaction);
                }
                else if (currentOrderItemsIds.Contains(item.WorkOrderItemId))
                {
                    // Update existing WorkOrderItem
                    await db.DapperProcQueryAsync<WorkOrderItemEntity>("Maintenance.WorkOrderItem_UpdateById", new
                    {
                        WorkOrderItemId = item.WorkOrderItemId,
                        WorkOrderId = workOrder.WorkOrderId,
                        Description = item.Description,
                        ReceiptId = item.ReceiptId,
                        LaborHours = item.LaborHours,
                        LaborCost = item.LaborCost,
                        ItemAmount = item.ItemAmount,
                        ModifiedBy = workOrder.ModifiedBy
                    }, transaction: transaction);
                }
            }

            // Delete WorkOrderItems that are no longer in the incoming list
            var workItemsToDelete = currentOrderItemsIds.Except(incomingOrderItemIds).ToList();
            foreach (var workOrderItemId in workItemsToDelete)
            {
                await db.DapperProcExecuteAsync("Maintenance.WorkOrderItem_DeleteById", new
                {
                    WorkOrderItemId = workOrderItemId
                }, transaction: transaction);
            }

            var updated = await LoadWorkOrderByIdAsync(db, transaction, workOrder.WorkOrderId, workOrder.OrganizationId);
            if (updated == null)
                throw new Exception("Work Order not updated");

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

    #region Deletes
    public async Task DeleteWorkOrderByIdAsync(Guid workOrderId, Guid organizationId, Guid modifiedBy)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.WorkOrder_DeleteById", new
        {
            WorkOrderId = workOrderId,
            OrganizationId = organizationId,
            modifiedBy = modifiedBy
        });
    }
    #endregion
}
