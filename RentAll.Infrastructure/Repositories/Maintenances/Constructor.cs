using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Serialization;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository : IMaintenanceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = SqlColumnJsonSerializerOptions.CaseInsensitive;
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
            PropertyCode = e.PropertyCode,
            InspectionCheckList = e.InspectionCheckList,
            Notes = e.Notes,
            IsActive = e.IsActive,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private static MaintenanceList ConvertEntityToModel(MaintenanceListEntity e)
    {
        return new MaintenanceList
        {
            MaintenanceId = e.MaintenanceId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            InspectionCheckList = e.InspectionCheckList,
            Bedroom1 = (BedSizeType)e.BedroomId1,
            Bedroom2 = (BedSizeType)e.BedroomId2,
            Bedroom3 = (BedSizeType)e.BedroomId3,
            Bedroom4 = (BedSizeType)e.BedroomId4,
            PetsAllowed = e.PetsAllowed,
            Notes = e.Notes,
            IsActive = e.IsActive,
            IsDeleted = e.IsDeleted,
            CreatedOn = e.CreatedOn,
            CreatedBy = e.CreatedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedBy = e.ModifiedBy
        };
    }

    private static Appliance ConvertEntityToModel(ApplianceEntity e)
    {
        return new Appliance
        {
            ApplianceId = e.ApplianceId,
            PropertyId = e.PropertyId,
            ApplianceName = e.ApplianceName,
            Manufacturer = e.Manufacturer,
            ModelNo = e.ModelNo,
            SerialNo = e.SerialNo,
            DecalPath = e.DecalPath
        };
    }

    private static Utility ConvertEntityToModel(UtilityEntity e)
    {
        return new Utility
        {
            UtilityId = e.UtilityId,
            PropertyId = e.PropertyId,
            UtilityName = e.UtilityName,
            Phone = e.Phone,
            AccountName = e.AccountName,
            AccountNumber = e.AccountNumber,
            Notes = e.Notes
        };
    }

    private static MaintenanceItem ConvertEntityToModel(MaintenanceItemEntity e)
    {
        return new MaintenanceItem
        {
            MaintenanceItemId = e.MaintenanceItemId,
            PropertyId = e.PropertyId,
            Name = e.Name,
            Notes = e.Notes,
            MonthsBetweenService = e.MonthsBetweenService,
            LastServicedOn = e.LastServicedOn
        };
    }

    private static Inspection ConvertEntityToModel(InspectionEntity e)
    {
        return new Inspection
        {
            InspectionId = e.InspectionId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            ReservationId = e.ReservationId,
            ReservationCode = e.ReservationCode,
            InspectionType = (InspectionType)e.InspectionTypeId,
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
        var splits = DeserializeReceiptSplits(e.Splits);

        return new Receipt
        {
            ReceiptId = e.ReceiptId,
            OrganizationId = e.OrganizationId,
            OfficeId = e.OfficeId,
            OfficeName = e.OfficeName,
            PropertyId = e.PropertyId,
            PropertyCode = e.PropertyCode,
            Amount = e.Amount,
            Description = e.Description,
            Splits = splits,
            ReceiptPath = e.ReceiptPath,
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedOn = e.CreatedOn,
            ModifiedBy = e.ModifiedBy,
            ModifiedOn = e.ModifiedOn,
            ModifiedByName = e.ModifiedByName
        };
    }

    private static List<ReceiptSplit> DeserializeReceiptSplits(string? splitsJson)
    {
        if (string.IsNullOrWhiteSpace(splitsJson))
            return new List<ReceiptSplit>();

        try
        {
            return JsonSerializer.Deserialize<List<ReceiptSplit>>(splitsJson, JsonOptions) ?? new List<ReceiptSplit>();
        }
        catch
        {
            return new List<ReceiptSplit>();
        }
    }

    private static string SerializeReceiptSplits(List<ReceiptSplit>? splits)
    {
        return JsonSerializer.Serialize(splits ?? new List<ReceiptSplit>(), JsonOptions);
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
            ApplyMarkup = e.ApplyMarkup,
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
