using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<MaintenanceList>> GetMaintenanceListByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceListEntity>("Maintenance.Maintenance_GetActiveListByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<MaintenanceList>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Maintenance?> GetMaintenanceByPropertyIdAsync(Guid propertyId, Guid organizationId, string offices)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_GetByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = offices
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Maintenance?> GetMaintenanceByIdAsync(Guid maintenanceId, Guid organizationId)
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
    #endregion

    #region Creates
    public async Task<Maintenance> CreateAsync(Maintenance maintenanceRecord)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<MaintenanceEntity>("Maintenance.Maintenance_Add", new
        {
            OrganizationId = maintenanceRecord.OrganizationId,
            OfficeId = maintenanceRecord.OfficeId,
            PropertyId = maintenanceRecord.PropertyId,
            InspectionCheckList = maintenanceRecord.InspectionCheckList,
            CleanerUserId = maintenanceRecord.CleanerUserId,
            CleaningDate = maintenanceRecord.CleaningDate,
            InspectorUserId = maintenanceRecord.InspectorUserId,
            InspectingDate = maintenanceRecord.InspectingDate,
            CarpetUserId = maintenanceRecord.CarpetUserId,
            CarpetDate = maintenanceRecord.CarpetDate,
            Notes = maintenanceRecord.Notes,
            IsActive = maintenanceRecord.IsActive,
            CreatedBy = maintenanceRecord.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance record not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
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
            CleanerUserId = maintenanceRecord.CleanerUserId,
            CleaningDate = maintenanceRecord.CleaningDate,
            InspectorUserId = maintenanceRecord.InspectorUserId,
            InspectingDate = maintenanceRecord.InspectingDate,
            CarpetUserId = maintenanceRecord.CarpetUserId,
            CarpetDate = maintenanceRecord.CarpetDate,
            Notes = maintenanceRecord.Notes,
            IsActive = maintenanceRecord.IsActive,
            ModifiedBy = maintenanceRecord.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Maintenance record not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteMaintenanceByIdAsync(Guid maintenanceId, Guid propertyId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Maintenance_DeleteById", new
        {
            MaintenanceId = maintenanceId,
            PropertyId = propertyId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
