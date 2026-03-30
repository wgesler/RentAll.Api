using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IMaintenanceRepository
{
    #region Maintenance
    Task<IEnumerable<MaintenanceList>> GetMaintenanceListByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<Maintenance?> GetMaintenanceByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Maintenance?> GetMaintenanceByIdAsync(Guid maintenanceId, Guid organizationId);

    Task<Maintenance> CreateAsync(Maintenance maintenanceRecord);
    Task<Maintenance> UpdateByIdAsync(Maintenance maintenanceRecord);
    Task DeleteMaintenanceByIdAsync(Guid maintenanceId, Guid organizationId, Guid modifiedBy);
    #endregion

    #region Appliance
    Task<IEnumerable<Appliance>> GetAppliancesByPropertyIdAsync(Guid propertyId);
    Task<Appliance?> GetApplianceByIdAsync(Guid applianceId);

    Task<Appliance> CreateApplianceAsync(Appliance appliance);
    Task<Appliance> UpdateApplianceAsync(Appliance appliance);
    Task DeleteApplianceByIdAsync(Guid applianceId);
    #endregion

    #region Inspection
    Task<IEnumerable<Inspection>> GetInspectionsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<IEnumerable<Inspection>> GetInspectionsByMaintenanceIdAsync(Guid maintenanceId, Guid organizationId, string officeAccess);
    Task<Inspection?> GetInspectionByIdAsync(int inspectionId, Guid organizationId);
    Task<Inspection?> GetInspectionByPropertyIdAsync(Guid propertyId, Guid organizationId, string offices);

    Task<Inspection> CreateInspectionAsync(Inspection inspection);
    Task<Inspection> UpdateInspectionByIdAsync(Inspection inspection);
    Task DeleteInspectionByIdAsync(int inspectionId, Guid organizationId);
    #endregion

    #region WorkOrder
    Task<IEnumerable<WorkOrder>> GetWorkOrdersByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<WorkOrder>> GetWorkOrdersByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<WorkOrder?> GetWorkOrderByIdAsync(Guid workOrderId, Guid organizationId);

    Task<WorkOrder> CreateWorkOrderAsync(WorkOrder workOrder);
    Task<WorkOrder> UpdateWorkOrderAsync(WorkOrder workOrder);
    Task DeleteWorkOrderByIdAsync(Guid workOrderId, Guid organizationId, Guid modifiedBy);
    #endregion

    #region Receipt
    Task<IEnumerable<Receipt>> GetReceiptsByOfficeIdsAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Receipt>> GetReceiptsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<Receipt?> GetReceiptByIdAsync(int receiptId, Guid organizationId);

    Task<Receipt> CreateReceiptAsync(Receipt receipt);
    Task<Receipt> UpdateReceiptAsync(Receipt receipt);
    Task DeleteReceiptByIdAsync(int receiptId, Guid organizationId, Guid currentUser);
    #endregion
}
