using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<MaintenanceItem>> GetMaintenanceItemsByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceItemEntity>("Maintenance.MaintenanceItem_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<MaintenanceItem>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<MaintenanceItem?> GetMaintenanceItemByIdAsync(int maintenanceItemId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceItemEntity>("Maintenance.MaintenanceItem_GetById", new
        {
            MaintenanceItemId = maintenanceItemId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<MaintenanceItem> CreateMaintenanceItemAsync(MaintenanceItem maintenanceItem)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceItemEntity>("Maintenance.MaintenanceItem_Add", new
        {
            PropertyId = maintenanceItem.PropertyId,
            Name = maintenanceItem.Name,
            Notes = maintenanceItem.Notes,
            MonthsBetweenService = maintenanceItem.MonthsBetweenService,
            LastServicedOn = maintenanceItem.LastServicedOn
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance item not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<MaintenanceItem> UpdateMaintenanceItemAsync(MaintenanceItem maintenanceItem)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceItemEntity>("Maintenance.MaintenanceItem_UpdateById", new
        {
            MaintenanceItemId = maintenanceItem.MaintenanceItemId,
            PropertyId = maintenanceItem.PropertyId,
            Name = maintenanceItem.Name,
            Notes = maintenanceItem.Notes,
            MonthsBetweenService = maintenanceItem.MonthsBetweenService,
            LastServicedOn = maintenanceItem.LastServicedOn
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance item not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteMaintenanceItemByIdAsync(int maintenanceItemId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.MaintenanceItem_DeleteById", new
        {
            MaintenanceItemId = maintenanceItemId
        });
    }
    #endregion
}
