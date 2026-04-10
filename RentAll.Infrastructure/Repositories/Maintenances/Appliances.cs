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

    public async Task<Appliance?> GetApplianceByIdAsync(int applianceId)
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
            PropertyId = appliance.PropertyId,
            ApplianceName = appliance.ApplianceName,
            Manufacturer = appliance.Manufacturer,
            ModelNo = appliance.ModelNo,
            SerialNo = appliance.SerialNo,
            DecalPath = appliance.DecalPath
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
            PropertyId = appliance.PropertyId,
            ApplianceName = appliance.ApplianceName,
            Manufacturer = appliance.Manufacturer,
            ModelNo = appliance.ModelNo,
            SerialNo = appliance.SerialNo,
            DecalPath = appliance.DecalPath
        });

        if (res == null || !res.Any())
            throw new Exception("Appliance not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteApplianceByIdAsync(int applianceId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Appliance_DeleteById", new
        {
            ApplianceId = applianceId
        });
    }
    #endregion
}
