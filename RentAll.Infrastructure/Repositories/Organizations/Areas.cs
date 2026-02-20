using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<Area> CreateAreaAsync(Area area)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_Add", new
        {
            OrganizationId = area.OrganizationId,
            OfficeId = area.OfficeId,
            AreaCode = area.AreaCode,
            Name = area.Name,
            Description = area.Description,
            IsActive = area.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Area not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<Area>> GetAllAreasAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_GetAll", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Area>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Area>> GetAllAreasByOfficeIdAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Area>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Area?> GetAreaByIdAsync(int areaId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_GetById", new
        {
            AreaId = areaId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Area?> GetAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_GetByCode", new
        {
            AreaCode = areaCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsAreaByCodeAsync(string areaCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Area_ExistsByCode", new
        {
            AreaCode = areaCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        return result == 1;
    }
    #endregion

    #region Update
    public async Task<Area> UpdateAreaByIdAsync(Area area)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<AreaEntity>("Organization.Area_UpdateById", new
        {
            AreaId = area.AreaId,
            OrganizationId = area.OrganizationId,
            OfficeId = area.OfficeId,
            AreaCode = area.AreaCode,
            Name = area.Name,
            Description = area.Description,
            IsActive = area.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Area not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteAreaByIdAsync(int areaId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Area_DeleteById", new
        {
            AreaId = areaId
        });
    }
    #endregion
}
