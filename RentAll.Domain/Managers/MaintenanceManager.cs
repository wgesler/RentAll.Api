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

    public async Task<Maintenance> UpdateByIdAsync(Maintenance maintenance, string officeAccess)
    {
        // If inspections already exist, retire and recreate; otherwise update in place.
        if (!await HasAssociatedInspections(maintenance.MaintenanceId, maintenance.OrganizationId, officeAccess))
            return await _maintenanceRepository.UpdateByIdAsync(maintenance);

        // Retire the old maintenance record
        var existing = await _maintenanceRepository.GetMaintenanceByIdAsync(maintenance.MaintenanceId, maintenance.OrganizationId);
        if (existing == null)
            throw new Exception($"Maintenance with ID {maintenance.MaintenanceId} not found.");

        existing.IsActive = false;
        await _maintenanceRepository.UpdateByIdAsync(existing);

        // Create a new maintenance record with the updated details
        return await _maintenanceRepository.CreateAsync(maintenance);
    }

    public async Task<bool> HasAssociatedInspections(Guid maintenanceId, Guid organizationId, string officeAccess)
    {
        var inspections = await _maintenanceRepository.GetInspectionsByMaintenanceIdAsync(maintenanceId, organizationId, officeAccess);
        return inspections.Any();
    }

    public async Task<bool> CurrentInspectionAlreadyExistsForProperty(Guid propertyId, Guid organizationId, string officeAccess)
    {
        var inspections = await _maintenanceRepository.GetInspectionsByPropertyIdAsync(propertyId, organizationId, officeAccess);
        return inspections.Any(o => o.IsActive);
    }

}
