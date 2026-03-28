using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Appliance>> GetAppliancesByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ApplianceEntity>("Maintenance.Appliance_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Appliance>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Appliance?> GetApplianceByIdAsync(Guid applianceId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ApplianceEntity>("Maintenance.Appliance_GetById", new
        {
            ApplianceId = applianceId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<Appliance> CreateApplianceAsync(Appliance appliance)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ApplianceEntity>("Maintenance.Appliance_Add", new
        {
            MaintenanceId = appliance.MaintenanceId,
            Name = appliance.Name,
            Make = appliance.Make,
            Model = appliance.Model,
            IsActive = appliance.IsActive,
            CreatedBy = appliance.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Appliance not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Appliance> UpdateApplianceAsync(Appliance appliance)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<ApplianceEntity>("Maintenance.Appliance_UpdateById", new
        {
            ApplianceId = appliance.ApplianceId,
            MaintenanceId = appliance.MaintenanceId,
            Name = appliance.Name,
            Make = appliance.Make,
            Model = appliance.Model,
            IsActive = appliance.IsActive,
            ModifiedBy = appliance.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Appliance not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteApplianceByIdAsync(Guid applianceId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Appliance_DeleteById", new
        {
            // Fyi - We don't store organization for appliance
            ApplianceId = applianceId
        });
    }
    #endregion
}
