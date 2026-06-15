using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<Feature>> GetFeaturesByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<FeatureEntity>("Organization.Feature_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Feature>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Feature?> GetFeatureByIdAsync(int featureId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<FeatureEntity>("Organization.Feature_GetById", new
        {
            FeatureId = featureId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsFeatureByOfficeAndFeatureTypeAsync(Guid organizationId, int officeId, int featureTypeId, int? excludeFeatureId = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Feature_ExistsByOfficeAndFeatureType", new
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            FeatureTypeId = featureTypeId,
            ExcludeFeatureId = excludeFeatureId
        });

        return result == 1;
    }
    #endregion

    #region Creates
    public async Task<Feature> CreateFeatureAsync(Feature feature)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<FeatureEntity>("Organization.Feature_Add", new
        {
            OrganizationId = feature.OrganizationId,
            OfficeId = feature.OfficeId,
            FeatureTypeId = (int)feature.FeatureTypeId,
            HasAccess = feature.HasAccess
        });

        if (res == null || !res.Any())
            throw new Exception("Feature not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<Feature> UpdateFeatureByIdAsync(Feature feature)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<FeatureEntity>("Organization.Feature_UpdateById", new
        {
            FeatureId = feature.FeatureId,
            OrganizationId = feature.OrganizationId,
            OfficeId = feature.OfficeId,
            FeatureTypeId = (int)feature.FeatureTypeId,
            HasAccess = feature.HasAccess
        });

        if (res == null || !res.Any())
            throw new Exception("Feature not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteFeatureByIdAsync(int featureId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Feature_DeleteById", new
        {
            FeatureId = featureId
        });
    }
    #endregion
}
