using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Organizations;

public partial class OrganizationRepository
{
    #region Selects
    public async Task<IEnumerable<Building>> GetBuildingsByOfficeIdsAsync(Guid organizationId, string officeAccess)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetAllByOfficeId", new
        {
            OrganizationId = organizationId,
            Offices = officeAccess
        });

        if (res == null || !res.Any())
            return Enumerable.Empty<Building>();

        return res.Select(ConvertEntityToModel);
    }

    public async Task<Building?> GetBuildingByIdAsync(int buildingId, Guid organizationId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetById", new
        {
            BuildingId = buildingId,
            OrganizationId = organizationId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<Building?> GetBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_GetByCode", new
        {
            BuildingCode = buildingCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        if (res == null || !res.Any())
            return null;

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }

    public async Task<bool> ExistsBuildingByCodeAsync(string buildingCode, Guid organizationId, int? officeId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var result = await db.DapperProcQueryScalarAsync<int>("Organization.Building_ExistsByCode", new
        {
            BuildingCode = buildingCode,
            OrganizationId = organizationId,
            OfficeId = officeId
        });

        return result == 1;
    }
    #endregion

    #region Creates
    public async Task<Building> CreateBuildingAsync(Building building)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_Add", new
        {
            OrganizationId = building.OrganizationId,
            OfficeId = building.OfficeId,
            BuildingCode = building.BuildingCode,
            Name = building.Name,
            Description = building.Description,
            HoaName = building.HoaName,
            HoaPhone = building.HoaPhone,
            HoaEmail = building.HoaEmail,
            Heating = building.Heating,
            Ac = building.Ac,
            Elevator = building.Elevator,
            Security = building.Security,
            Gated = building.Gated,
            PetsAllowed = building.PetsAllowed,
            DogsOkay = building.DogsOkay,
            CatsOkay = building.CatsOkay,
            PoundLimit = building.PoundLimit,
            TrashPickupId = building.TrashPickupId,
            TrashRemoval = building.TrashRemoval,
            WasherDryerInBldg = building.WasherDryerInBldg,
            Deck = building.Deck,
            Patio = building.Patio,
            Yard = building.Yard,
            Garden = building.Garden,
            CommonPool = building.CommonPool,
            PrivatePool = building.PrivatePool,
            Jacuzzi = building.Jacuzzi,
            Sauna = building.Sauna,
            Gym = building.Gym,
            IsActive = building.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Building not created");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Updates
    public async Task<Building> UpdateBuildingByIdAsync(Building building)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var res = await db.DapperProcQueryAsync<BuildingEntity>("Organization.Building_UpdateById", new
        {
            BuildingId = building.BuildingId,
            OrganizationId = building.OrganizationId,
            OfficeId = building.OfficeId,
            BuildingCode = building.BuildingCode,
            Name = building.Name,
            Description = building.Description,
            HoaName = building.HoaName,
            HoaPhone = building.HoaPhone,
            HoaEmail = building.HoaEmail,
            Heating = building.Heating,
            Ac = building.Ac,
            Elevator = building.Elevator,
            Security = building.Security,
            Gated = building.Gated,
            PetsAllowed = building.PetsAllowed,
            DogsOkay = building.DogsOkay,
            CatsOkay = building.CatsOkay,
            PoundLimit = building.PoundLimit,
            TrashPickupId = building.TrashPickupId,
            TrashRemoval = building.TrashRemoval,
            WasherDryerInBldg = building.WasherDryerInBldg,
            Deck = building.Deck,
            Patio = building.Patio,
            Yard = building.Yard,
            Garden = building.Garden,
            CommonPool = building.CommonPool,
            PrivatePool = building.PrivatePool,
            Jacuzzi = building.Jacuzzi,
            Sauna = building.Sauna,
            Gym = building.Gym,
            IsActive = building.IsActive
        });

        if (res == null || !res.Any())
            throw new Exception("Building not found");

        return ConvertEntityToModel(res.FirstOrDefault()!);
    }
    #endregion

    #region Deletes
    public async Task DeleteBuildingByIdAsync(int buildingId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        await db.DapperProcExecuteAsync("Organization.Building_DeleteById", new
        {
            BuildingId = buildingId
        });
    }
    #endregion
}
