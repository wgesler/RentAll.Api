using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Create
    public async Task<Region> CreateRegionAsync(Region region)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_Add", new
        {
            OrganizationId = region.OrganizationId,
            OfficeId = region.OfficeId,
            RegionCode = region.RegionCode,
            Name = region.Name,
            Description = region.Description,
            IsActive = region.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Region not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Select
    public async Task<IEnumerable<Region>> GetAllRegionsAsync(Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetAll", new
        {
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Region>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<Region>> GetAllRegionsByOfficeIdAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Region>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Region?> GetRegionByIdAsync(int regionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetById", new
        {
            RegionId = regionId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Region?> GetRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_GetByCode", new
        {
            RegionCode = regionCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsRegionByCodeAsync(string regionCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Region_ExistsByCode", new
        {
            RegionCode = regionCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        return result == 1;
    }
    #endregion

    #region Update
    public async Task<Region> UpdateRegionByIdAsync(Region region)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<RegionEntity>("Organization.Region_UpdateById", new
        {
            RegionId = region.RegionId,
            OrganizationId = region.OrganizationId,
            OfficeId = region.OfficeId,
            RegionCode = region.RegionCode,
            Name = region.Name,
            Description = region.Description,
            IsActive = region.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Region not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Delete
    public async Task DeleteRegionByIdAsync(int regionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Region_DeleteById", new
        {
            RegionId = regionId
        });
    }
    #endregion
}
