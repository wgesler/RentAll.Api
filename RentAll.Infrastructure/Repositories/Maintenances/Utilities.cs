using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Utility>> GetUtilitiesByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UtilityEntity>("Maintenance.Utility_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Utility>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Utility?> GetUtilityByIdAsync(int utilityId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UtilityEntity>("Maintenance.Utility_GetById", new
        {
            UtilityId = utilityId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Creates
    public async Task<Utility> CreateUtilityAsync(Utility utility)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UtilityEntity>("Maintenance.Utility_Add", new
        {
            PropertyId = utility.PropertyId,
            UtilityName = utility.UtilityName,
            Phone = utility.Phone,
            AccountName = utility.AccountName,
            AccountNumber = utility.AccountNumber,
            Notes = utility.Notes
        });

        if (res == null || !res.Any())
            throw new Exception("Utility not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Utility> UpdateUtilityAsync(Utility utility)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<UtilityEntity>("Maintenance.Utility_UpdateById", new
        {
            UtilityId = utility.UtilityId,
            PropertyId = utility.PropertyId,
            UtilityName = utility.UtilityName,
            Phone = utility.Phone,
            AccountName = utility.AccountName,
            AccountNumber = utility.AccountNumber,
            Notes = utility.Notes
        });

        if (res == null || !res.Any())
            throw new Exception("Utility not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteUtilityByIdAsync(int utilityId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Utility_DeleteById", new
        {
            UtilityId = utilityId
        });
    }
    #endregion
}
