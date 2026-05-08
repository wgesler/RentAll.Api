using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties;

public partial class PropertyRepository
{
    #region Selects
    public async Task<IEnumerable<TrackerResponse>> GetTrackerResponsesByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseEntity>("Property.TrackerResponse_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerResponse>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<TrackerResponseOption>> GetTrackerResponseOptionsByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseOptionEntity>("Property.TrackerResponseOption_GetByPropertyId", new
        {
            PropertyId = propertyId
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerResponseOption>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<IEnumerable<TrackerResponse>> GetTrackerResponsesByOfficeIdsAsync(Guid organizationId, string officeAccess, bool includeInactive = false, bool excludeCompletedPropertyTracking = true)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseEntity>("Property.TrackerResponse_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess,
            IncludeInactive = includeInactive,
            ExcludeCompletedPropertyTracking = excludeCompletedPropertyTracking
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerResponse>();

        return res
            .Select(ConvertEntityToModel)
            .Where(r => r.PropertyId != Guid.Empty);
    }

    public async Task<IEnumerable<TrackerResponseOption>> GetTrackerResponseOptionsByOfficeIdsAsync(Guid organizationId, string officeAccess, bool includeInactive = false, bool excludeCompletedPropertyTracking = true)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseOptionEntity>("Property.TrackerResponseOption_GetAllByOfficeIds", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess,
            IncludeInactive = includeInactive,
            ExcludeCompletedPropertyTracking = excludeCompletedPropertyTracking
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<TrackerResponseOption>();

        return res
            .Select(ConvertEntityToModel)
            .Where(r => r.PropertyId != Guid.Empty);
    }

    public async Task<TrackerResponse?> GetTrackerResponseByIdAsync(Guid trackerResponseId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseEntity>("Property.TrackerResponse_GetById", new
        {
            TrackerResponseId = trackerResponseId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerResponseOption?> GetTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseOptionEntity>("Property.TrackerResponseOption_GetById", new
        {
            TrackerResponseId = trackerResponseId,
            TrackerDefinitionOptionId = trackerDefinitionOptionId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Creates
    public async Task<TrackerResponse> CreateTrackerResponseAsync(TrackerResponse trackerResponse)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseEntity>("Property.TrackerResponse_Add", new
        {
            TrackerDefinitionId = trackerResponse.TrackerDefinitionId,
            PropertyId = trackerResponse.PropertyId,
            ReservationId = trackerResponse.ReservationId,
            EntityTypeId = trackerResponse.EntityTypeId,
            EntityId = trackerResponse.EntityId,
            IsChecked = trackerResponse.IsChecked,
            CheckedOn = trackerResponse.CheckedOn,
            CheckedBy = trackerResponse.CheckedBy,
            CreatedBy = trackerResponse.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker response not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerResponseOption> CreateTrackerResponseOptionAsync(TrackerResponseOption trackerResponseOption)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseOptionEntity>("Property.TrackerResponseOption_Add", new
        {
            TrackerResponseId = trackerResponseOption.TrackerResponseId,
            TrackerDefinitionOptionId = trackerResponseOption.TrackerDefinitionOptionId,
            CreatedBy = trackerResponseOption.CreatedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker response option not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<TrackerResponse> UpdateTrackerResponseByIdAsync(TrackerResponse trackerResponse)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseEntity>("Property.TrackerResponse_UpdateById", new
        {
            TrackerResponseId = trackerResponse.TrackerResponseId,
            TrackerDefinitionId = trackerResponse.TrackerDefinitionId,
            PropertyId = trackerResponse.PropertyId,
            ReservationId = trackerResponse.ReservationId,
            EntityTypeId = trackerResponse.EntityTypeId,
            EntityId = trackerResponse.EntityId,
            IsChecked = trackerResponse.IsChecked,
            CheckedOn = trackerResponse.CheckedOn,
            CheckedBy = trackerResponse.CheckedBy,
            ModifiedBy = trackerResponse.ModifiedBy
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker response not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<TrackerResponseOption> UpdateTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId, Guid newTrackerDefinitionOptionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<TrackerResponseOptionEntity>("Property.TrackerResponseOption_UpdateById", new
        {
            TrackerResponseId = trackerResponseId,
            TrackerDefinitionOptionId = trackerDefinitionOptionId,
            NewTrackerDefinitionOptionId = newTrackerDefinitionOptionId
        });

        if (res == null || !res.Any())
            throw new Exception("Tracker response option not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteTrackerResponsesByPropertyIdAsync(Guid propertyId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.TrackerResponse_DeleteByPropertyId", new
        {
            PropertyId = propertyId
        });
    }

    public async Task DeleteTrackerResponseByIdAsync(Guid trackerResponseId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.TrackerResponse_DeleteById", new
        {
            TrackerResponseId = trackerResponseId
        });
    }

    public async Task DeleteTrackerResponseOptionByIdAsync(Guid trackerResponseId, Guid trackerDefinitionOptionId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Property.TrackerResponseOption_DeleteById", new
        {
            TrackerResponseId = trackerResponseId,
            TrackerDefinitionOptionId = trackerDefinitionOptionId
        });
    }
    #endregion
}
