using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Domain.Enums;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<TrackerContext>> GetTrackerContextsAsync()
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerContextEntity>("Organization.TrackerContext_GetAll");

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerContext>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<TrackerContext?> GetTrackerContextByIdAsync(int trackerContextId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerContextEntity>("Organization.TrackerContext_GetById", new
        {
            TrackerContextId = trackerContextId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<IEnumerable<TrackerDefinition>> GetTrackerDefinitionsByOfficeIdsAsync(Guid organizationId, string officeAccess, int? trackerContextId, bool includeInactive)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionEntity>("Organization.TrackerDefinition_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess,
            TrackerContextId = trackerContextId,
            IncludeInactive = includeInactive
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerDefinition>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<TrackerDefinition?> GetTrackerDefinitionByIdAsync(Guid trackerDefinitionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionEntity>("Organization.TrackerDefinition_GetById", new
        {
            TrackerDefinitionId = trackerDefinitionId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<IEnumerable<TrackerDefinitionOption>> GetTrackerDefinitionOptionsByOfficeIdsAsync(Guid organizationId, string officeAccess, int? trackerContextId, bool includeInactive)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionOptionEntity>("Organization.TrackerDefinitionOption_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess,
            TrackerContextId = trackerContextId,
            IncludeInactive = includeInactive
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerDefinitionOption>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<TrackerDefinitionOption>> GetTrackerDefinitionOptionsByTrackerDefinitionIdAsync(Guid trackerDefinitionId, bool includeInactive)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionOptionEntity>("Organization.TrackerDefinitionOption_GetByTrackerDefinitionId", new
        {
            TrackerDefinitionId = trackerDefinitionId,
            IncludeInactive = includeInactive
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerDefinitionOption>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<TrackerDefinitionOption?> GetTrackerDefinitionOptionByIdAsync(Guid trackerDefinitionOptionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionOptionEntity>("Organization.TrackerDefinitionOption_GetById", new
        {
            TrackerDefinitionOptionId = trackerDefinitionOptionId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<TrackerContext> CreateTrackerContextAsync(TrackerContext trackerContext)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerContextEntity>("Organization.TrackerContext_Add", new
        {
            TrackerContextId = (int)trackerContext.TrackerContextId,
            Code = trackerContext.Code,
            Description = trackerContext.Description,
            IsActive = trackerContext.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker context not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerDefinition> CreateTrackerDefinitionAsync(TrackerDefinition trackerDefinition)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionEntity>("Organization.TrackerDefinition_Add", new
        {
            OrganizationId = trackerDefinition.OrganizationId,
            OfficeId = trackerDefinition.OfficeId,
            TrackerContextId = (int)trackerDefinition.TrackerContextId,
            DisplayName = trackerDefinition.DisplayName,
            Description = trackerDefinition.Description,
            SortOrder = trackerDefinition.SortOrder,
            IsActive = trackerDefinition.IsActive,
            CreatedBy = trackerDefinition.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker definition not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerDefinitionOption> CreateTrackerDefinitionOptionAsync(TrackerDefinitionOption trackerDefinitionOption)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionOptionEntity>("Organization.TrackerDefinitionOption_Add", new
        {
            TrackerDefinitionId = trackerDefinitionOption.TrackerDefinitionId,
            Label = trackerDefinitionOption.Label,
            Description = trackerDefinitionOption.OptionDescription,
            SortOrder = trackerDefinitionOption.OptionSortOrder,
            IsActive = trackerDefinitionOption.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker definition option not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<TrackerContext> UpdateTrackerContextByIdAsync(TrackerContext trackerContext)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerContextEntity>("Organization.TrackerContext_UpdateById", new
        {
            TrackerContextId = (int)trackerContext.TrackerContextId,
            Code = trackerContext.Code,
            Description = trackerContext.Description,
            IsActive = trackerContext.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker context not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerDefinition> UpdateTrackerDefinitionByIdAsync(TrackerDefinition trackerDefinition)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionEntity>("Organization.TrackerDefinition_UpdateById", new
        {
            TrackerDefinitionId = trackerDefinition.TrackerDefinitionId,
            OrganizationId = trackerDefinition.OrganizationId,
            OfficeId = trackerDefinition.OfficeId,
            TrackerContextId = (int)trackerDefinition.TrackerContextId,
            DisplayName = trackerDefinition.DisplayName,
            Description = trackerDefinition.Description,
            SortOrder = trackerDefinition.SortOrder,
            IsActive = trackerDefinition.IsActive,
            ModifiedBy = trackerDefinition.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker definition not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerDefinitionOption> UpdateTrackerDefinitionOptionByIdAsync(TrackerDefinitionOption trackerDefinitionOption)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerDefinitionOptionEntity>("Organization.TrackerDefinitionOption_UpdateById", new
        {
            TrackerDefinitionOptionId = trackerDefinitionOption.TrackerDefinitionOptionId,
            TrackerDefinitionId = trackerDefinitionOption.TrackerDefinitionId,
            Label = trackerDefinitionOption.Label,
            Description = trackerDefinitionOption.OptionDescription,
            SortOrder = trackerDefinitionOption.OptionSortOrder,
            IsActive = trackerDefinitionOption.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker definition option not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteTrackerContextByIdAsync(int trackerContextId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.TrackerContext_DeleteById", new
        {
            TrackerContextId = trackerContextId
        });
    }

    public async Task DeleteTrackerDefinitionByIdAsync(Guid trackerDefinitionId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.TrackerDefinition_DeleteById", new
        {
            TrackerDefinitionId = trackerDefinitionId,
            OrganizationId = organizationId
        });
    }

    public async Task DeleteTrackerDefinitionOptionByIdAsync(Guid trackerDefinitionOptionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.TrackerDefinitionOption_DeleteById", new
        {
            TrackerDefinitionOptionId = trackerDefinitionOptionId
        });
    }
    #endregion

}

