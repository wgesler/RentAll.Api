using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    // Inventory - Creates
    public async Task<Inventory> CreateInventoryAsync(Inventory inventory)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InventoryEntity>("Maintenance.Inventory_Add", new
        {
            OrganizationId = inventory.OrganizationId,
            OfficeId = inventory.OfficeId,
            PropertyId = inventory.PropertyId,
            MaintenanceId = inventory.MaintenanceId,
            InventoryCheckList = inventory.InventoryCheckList,
            DocumentPath = inventory.DocumentPath,
            IsActive = inventory.IsActive,
            CreatedBy = inventory.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inventory record not created");

        return ConvertEntityToModel(res.First());
    }

    // Inventory - Selects
    public async Task<IEnumerable<Inventory>> GetInventoriesByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InventoryListEntity>("Maintenance.Inventory_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Inventory>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Inventory>> GetInventoriesByMaintenanceIdAsync(Guid maintenanceId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InventoryListEntity>("Maintenance.Inventory_GetListByMaintenanceId", new
        {
            MaintenanceId = maintenanceId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Inventory>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Inventory?> GetInventoryByIdAsync(int inventoryId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InventoryEntity>("Maintenance.Inventory_GetById", new
        {
            InventoryId = inventoryId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    // Inventory - Updates
    public async Task<Inventory> UpdateInventoryByIdAsync(Inventory inventory)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InventoryEntity>("Maintenance.Inventory_UpdateById", new
        {
            InventoryId = inventory.InventoryId,
            OrganizationId = inventory.OrganizationId,
            OfficeId = inventory.OfficeId,
            PropertyId = inventory.PropertyId,
            MaintenanceId = inventory.MaintenanceId,
            InventoryCheckList = inventory.InventoryCheckList,
            DocumentPath = inventory.DocumentPath,
            IsActive = inventory.IsActive,
            ModifiedBy = inventory.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inventory record not found");

        return ConvertEntityToModel(res.First());
    }

    // Inventory - Deletes
    public async Task DeleteInventoryByIdAsync(int inventoryId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Inventory_DeleteById", new
        {
            InventoryId = inventoryId,
            OrganizationId = organizationId
        });
    }
}
