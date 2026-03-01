using RentAll.Domain.Models.Maintenances;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IMaintenanceRepository
{
    #region Maintenance
    // Creates
    Task<Maintenance> CreateAsync(Maintenance maintenanceRecord);

    // Selects
    Task<Maintenance?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId, Guid? maintenanceId = null);
    Task<Maintenance?> GetByIdAsync(Guid maintenanceId, Guid organizationId);

    // Updates
    Task<Maintenance> UpdateByIdAsync(Maintenance maintenanceRecord);

    // Deletes
    Task DeleteByIdAsync(Guid maintenanceId, Guid organizationId, Guid modifiedBy);
    #endregion


    #region Inventory
    // Creates
    Task<Inventory> CreateInventoryAsync(Inventory inventory);

    // Selects
    Task<IEnumerable<Inventory>> GetInventoriesByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess);
    Task<IEnumerable<Inventory>> GetInventoriesByMaintenanceIdAsync(Guid maintenanceId, Guid organizationId, string officeAccess);
    Task<Inventory?> GetInventoryByIdAsync(int inventoryId, Guid organizationId);

    // Updates
    Task<Inventory> UpdateInventoryByIdAsync(Inventory inventory);

    // Deletes
    Task DeleteInventoryByIdAsync(int inventoryId, Guid organizationId);
    #endregion

}
