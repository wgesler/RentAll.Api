using Microsoft.Data.SqlClient;
using RentAll.Infrastructure.Configuration;
using RentAll.Domain.Models;

namespace RentAll.Infrastructure.Repositories.Maintenances;

public partial class MaintenanceRepository
{
    #region Selects
    public async Task<IEnumerable<Inspection>> GetInspectionsByPropertyIdAsync(Guid propertyId, Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_GetByPropertyId", new
        {
            PropertyId = propertyId,
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

    public async Task<Inspection?> GetInspectionByPropertyIdAsync(Guid property, Guid organizationId, string offices)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_GetByPropertyId", new
        {
            PropertyId = property,
            OrganizationId = organizationId,
            Offices = offices
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.First());
    }

    #endregion

    #region Creates
    public async Task<Inspection> CreateInspectionAsync(Inspection inspection)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_Add", new
        {
            OrganizationId = inspection.OrganizationId,
            OfficeId = inspection.OfficeId,
            PropertyId = inspection.PropertyId,
            InspectionTypeId = (int)inspection.InspectionType,
            InspectionCheckList = inspection.InspectionCheckList,
            DocumentPath = inspection.DocumentPath,
            IsActive = inspection.IsActive,
            CreatedBy = inspection.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inspection record not created");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Updates
    public async Task<Inspection> UpdateInspectionByIdAsync(Inspection inspection)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<InspectionEntity>("Maintenance.Inspection_UpdateById", new
        {
            InspectionId = inspection.InspectionId,
            OrganizationId = inspection.OrganizationId,
            OfficeId = inspection.OfficeId,
            PropertyId = inspection.PropertyId,
            InspectionTypeId = (int)inspection.InspectionType,
            InspectionCheckList = inspection.InspectionCheckList,
            DocumentPath = inspection.DocumentPath,
            IsActive = inspection.IsActive,
            ModifiedBy = inspection.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Inspection record not found");

        return ConvertEntityToModel(res.First());
    }
    #endregion

    #region Deletes
    public async Task DeleteInspectionByIdAsync(int inspectionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Maintenance.Inspection_DeleteById", new
        {
            InspectionId = inspectionId,
            OrganizationId = organizationId
        });
    }
    #endregion
}
