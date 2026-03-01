using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    // Creates
    public async Task<Maintenance> CreateAsync(Maintenance maintenanceRecord)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_Add", new
        {
            OrganizationId = maintenanceRecord.OrganizationId,
            OfficeId = maintenanceRecord.OfficeId,
            PropertyId = maintenanceRecord.PropertyId,
            InspectionCheckList = maintenanceRecord.InspectionCheckList,
            InventoryCheckList = maintenanceRecord.InventoryCheckList,
            Notes = maintenanceRecord.Notes,
            IsActive = maintenanceRecord.IsActive,
            CreatedBy = maintenanceRecord.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance record not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    // Selects

    public async Task<Maintenance?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId, Guid? maintenanceId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_GetByPropertyId", new
        {
            MaintenanceId = maintenanceId,
            PropertyId = propertyId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Maintenance?> GetByIdAsync(Guid maintenanceId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_GetById", new
        {
            MaintenanceId = maintenanceId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    // Updates
    public async Task<Maintenance> UpdateByIdAsync(Maintenance maintenanceRecord)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_UpdateById", new
        {
            MaintenanceId = maintenanceRecord.MaintenanceId,
            OrganizationId = maintenanceRecord.OrganizationId,
            OfficeId = maintenanceRecord.OfficeId,
            PropertyId = maintenanceRecord.PropertyId,
            InspectionCheckList = maintenanceRecord.InspectionCheckList,
            InventoryCheckList = maintenanceRecord.InventoryCheckList,
            Notes = maintenanceRecord.Notes,
            IsActive = maintenanceRecord.IsActive,
            ModifiedBy = maintenanceRecord.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance record not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    // Deletes
    public async Task DeleteByIdAsync(Guid maintenanceId, Guid propertyId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Maintenance_DeleteById", new
        {
            MaintenanceId = maintenanceId,
            PropertyId = propertyId,
            OrganizationId = organizationId
        });
    }
}
