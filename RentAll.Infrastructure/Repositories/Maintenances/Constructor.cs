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
            PropertyCode = e.PropertyCode,
            InspectionCheckList = e.InspectionCheckList,
            CleanerUserId = e.CleanerUserId,
            CleaningDate = e.CleaningDate,
            InspectorUserId = e.InspectorUserId,
            InspectingDate = e.InspectingDate,
            CarpetUserId = e.CarpetUserId,
            CarpetDate = e.CarpetDate,
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
            CleanerUserId = e.CleanerUserId,
            CleaningDate = e.CleaningDate,
            InspectorUserId = e.InspectorUserId,
            InspectingDate = e.InspectingDate,
            CarpetUserId = e.CarpetUserId,
            CarpetDate = e.CarpetDate,
            Bedroom1 = (BedSizeType)e.BedroomId1,
            Bedroom2 = (BedSizeType)e.BedroomId2,
            Bedroom3 = (BedSizeType)e.BedroomId3,
            Bedroom4 = (BedSizeType)e.BedroomId4,
            PetsAllowed = e.PetsAllowed,
            FilterDescription = e.FilterDescription,
            LastFilterChangeDate = e.LastFilterChangeDate,
            SmokeDetectors = e.SmokeDetectors,
            LastSmokeChangeDate = e.LastSmokeChangeDate,
            SmokeDetectorBatteries = e.SmokeDetectorBatteries,
            LastBatteryChangeDate = e.LastBatteryChangeDate,
            LicenseNo = e.LicenseNo,
            LicenseDate = e.LicenseDate,
            HvacNotes = e.HvacNotes,
            HvacServiced = e.HvacServiced,
            FireplaceNotes = e.FireplaceNotes,
            FireplaceServiced = e.FireplaceServiced,
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
            SerialNo = e.SerialNo
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
