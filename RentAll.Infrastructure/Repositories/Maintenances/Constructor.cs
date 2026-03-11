using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository : IMaintenanceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions{ PropertyNameCaseInsensitive = true };
    private readonly string _dbConnectionString;

    public MaintenanceRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    private static Maintenance ConvertEntityToModel(MaintenanceEntity e)
    {
        return new Maintenance
        {
            MaintenanceId = e.MaintenanceId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            InspectionCheckList = e.InspectionCheckList,
            InventoryCheckList = e.InventoryCheckList,
            Notes = e.Notes,
            IsActive = e.IsActive,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private static Inventory ConvertEntityToModel(InventoryEntity e)
    {
        return new Inventory
        {
            InventoryId = e.InventoryId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            MaintenanceId = e.MaintenanceId,
            InventoryCheckList = e.InventoryCheckList,
            DocumentPath = e.DocumentPath,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static Inspection ConvertEntityToModel(InspectionEntity e)
    {
        return new Inspection
        {
            InspectionId = e.InspectionId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            PropertyId = e.PropertyId,
            MaintenanceId = e.MaintenanceId,
            InspectionCheckList = e.InspectionCheckList,
            DocumentPath = e.DocumentPath,
            IsActive = e.IsActive,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static Receipt ConvertEntityToModel(ReceiptEntity e)
    {
        return new Receipt
        {
            ReceiptId = e.ReceiptId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            Description = e.Description,
            Amount = e.Amount,
            ReceiptPath = e.ReceiptPath,
            WorkOrderCode = e.WorkOrderCode,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static WorkOrder ConvertEntityToModel(WorkOrderEntity e)
    {
        List<WorkOrderItem> items = new List<WorkOrderItem>();
        if (!string.IsNullOrWhiteSpace(e.WorkOrderItems))
        {
            try
            {
                var entityItems = JsonSerializer.Deserialize<List<WorkOrderItemEntity>>(e.WorkOrderItems, JsonOptions) ?? new List<WorkOrderItemEntity>();
                items = entityItems.Select(ConvertWorkOrderItemEntityToModel).ToList();
            }
            catch
            {
                items = new List<WorkOrderItem>();
            }
        }

        return new WorkOrder
        {
            WorkOrderId = e.WorkOrderId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            WorkOrderCode = e.WorkOrderCode,
            Description = e.Description,
            WorkOrderType = (WorkOrderType)e.WorkOrderTypeId,
            WorkOrderItems = items,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static WorkOrderItem ConvertWorkOrderItemEntityToModel(WorkOrderItemEntity e)
    {
        return new WorkOrderItem
        {
            WorkOrderItemId = e.WorkOrderItemId,
            WorkOrderId = e.WorkOrderId,
            Description = e.Description,
            ReceiptId = e.ReceiptId,
            LaborHours = e.LaborHours,
            LaborCost = e.LaborCost,
            ItemAmount = e.ItemAmount
        };
    }
}
