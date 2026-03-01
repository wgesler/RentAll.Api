using RentAll.Domain.Models.Maintenances;

namespace RentAll.Domain.Interfaces.Managers;

public interface IMaintenanceManager
{
    Task<Maintenance> UpdateByIdAsync(Maintenance maintenanceRecord, string officeAccess);
    Task<bool> HasAssociatedInventories(Guid maintenanceId, Guid organizationId, string officeAccess);
    Task<bool> CurrentInventoryAlreadyExistsForProperty(Guid propertyId, Guid organizationId, string officeAccess);
}
