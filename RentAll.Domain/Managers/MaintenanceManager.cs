using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public class MaintenanceManager : IMaintenanceManager
{
    private readonly IMaintenanceRepository _maintenanceRepository;

    public MaintenanceManager(IMaintenanceRepository maintenanceRepository)
    {
        _maintenanceRepository = maintenanceRepository;
    }

    public async Task<Maintenance> UpdateByIdAsync(Maintenance m, string officeAccess)
    {
        // If inspections already exist, retire and recreate; otherwise update in place.
        var inspections = await _maintenanceRepository.GetInspectionsByPropertyIdAsync(m.PropertyId, m.OrganizationId, officeAccess);
        if (!inspections.Any())
            return await _maintenanceRepository.UpdateByIdAsync(m);

        // Retire the old maintenance record
        foreach (var inspection in inspections)
        {
            inspection.IsActive = false;
            await _maintenanceRepository.UpdateInspectionByIdAsync(inspection);
        }

        // Create a new maintenance record with the updated details
        m.CreatedBy = m.ModifiedBy;
        return await _maintenanceRepository.CreateAsync(m);
    }

    public async Task<bool> HasAssociatedInspections(Guid propertyId, Guid organizationId, string officeAccess)
    {
        var inspections = await _maintenanceRepository.GetInspectionsByPropertyIdAsync(propertyId, organizationId, officeAccess);
        return inspections.Any();
    }

    public async Task<bool> CurrentInspectionAlreadyExistsForProperty(Guid propertyId, Guid organizationId, string officeAccess)
    {
        var inspections = await _maintenanceRepository.GetInspectionsByPropertyIdAsync(propertyId, organizationId, officeAccess);
        return inspections.Any(o => o.IsActive);
    }

}
