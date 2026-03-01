using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Domain.Models.Maintenances;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    // Inspection - Creates
    public async Task<Inspection> CreateInspectionAsync(Inspection inspection)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_Add", new
        {
            OrganizationId = inspection.OrganizationId,
            OfficeId = inspection.OfficeId,
            PropertyId = inspection.PropertyId,
            MaintenanceId = inspection.MaintenanceId,
            InspectionCheckList = inspection.InspectionCheckList,
            DocumentPath = inspection.DocumentPath,
            IsActive = inspection.IsActive,
            CreatedBy = inspection.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inspection record not created");

        return ConvertEntityToModel(res.First());
    }

    // Inspection - Selects
    public async Task<IEnumerable<Inspection>> GetInspectionsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionListEntity>("Maintenance.Inspection_GetListByPropertyId", new
        {
            PropertyId = propertyId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Inspection>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Inspection>> GetInspectionsByMaintenanceIdAsync(Guid maintenanceId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionListEntity>("Maintenance.Inspection_GetListByMaintenanceId", new
        {
            MaintenanceId = maintenanceId,
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Inspection>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Inspection?> GetInspectionByIdAsync(int inspectionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_GetById", new
        {
            InspectionId = inspectionId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    // Inspection - Updates
    public async Task<Inspection> UpdateInspectionByIdAsync(Inspection inspection)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_UpdateById", new
        {
            InspectionId = inspection.InspectionId,
            OrganizationId = inspection.OrganizationId,
            OfficeId = inspection.OfficeId,
            PropertyId = inspection.PropertyId,
            MaintenanceId = inspection.MaintenanceId,
            InspectionCheckList = inspection.InspectionCheckList,
            DocumentPath = inspection.DocumentPath,
            IsActive = inspection.IsActive,
            ModifiedBy = inspection.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inspection record not found");

        return ConvertEntityToModel(res.First());
    }

    // Inspection - Deletes
    public async Task DeleteInspectionByIdAsync(int inspectionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Inspection_DeleteById", new
        {
            InspectionId = inspectionId,
            OrganizationId = organizationId
        });
    }
}
