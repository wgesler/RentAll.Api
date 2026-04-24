using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
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
        var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetById", new
        {
            WorkOrderId = workOrderId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
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
                Description = workOrder.Description,
                WorkOrderTypeId = (int)workOrder.WorkOrderType,
                ApplyMarkup = workOrder.ApplyMarkup,
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

            // Get fully populated work order
            var res = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetById", new
            {
                WorkOrderId = o.WorkOrderId,
                OrganizationId = o.OrganizationId
            }, transaction: transaction);

            if (res == null || !res.Any())
                throw new Exception("Work Order not found");

            await transaction.CommitAsync();
            return ConvertEntityToModel(res.FirstOrDefault()!);
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
            // Get fully populated work order
            var existing = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetById", new
            {
                WorkOrderId = workOrder.WorkOrderId,
                OrganizationId = workOrder.OrganizationId
            }, transaction: transaction);

            if (existing == null || !existing.Any())
                throw new Exception("Work Order not found");

            var currentOrder = ConvertEntityToModel(existing.FirstOrDefault()!);
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
                Description = workOrder.Description,
                WorkOrderTypeId = (int)workOrder.WorkOrderType,
                ApplyMarkup = workOrder.ApplyMarkup,
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

            // Get fully populated work order
            var updated = await db.DapperProcQueryAsync<WorkOrderEntity>("Maintenance.WorkOrder_GetById", new
            {
                WorkOrderId = workOrder.WorkOrderId,
                OrganizationId = workOrder.OrganizationId
            }, transaction: transaction);

            if (updated == null || !updated.Any())
                throw new Exception("Work Order not updated");

            await transaction.CommitAsync();
            return ConvertEntityToModel(updated.FirstOrDefault()!);
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
