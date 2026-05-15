using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using System.Text.Json;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Selects
        public async Task<PropertySelection?> GetPropertySelectionByUserIdAsync(Guid userId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertySelectionEntity>("Property.PropertySelection_GetByUserId", new
            {
                UserId = userId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.First()!);
        }
        #endregion

        #region Creates
        public async Task<PropertySelection> UpsertPropertySelectionAsync(PropertySelection selection)
        {
            var buildingCodesJson = selection.BuildingCodes != null && selection.BuildingCodes.Any()
                ? JsonSerializer.Serialize(selection.BuildingCodes)
                : "[]";
            var regionCodesJson = selection.RegionCodes != null && selection.RegionCodes.Any()
                ? JsonSerializer.Serialize(selection.RegionCodes)
                : "[]";
            var areaCodesJson = selection.AreaCodes != null && selection.AreaCodes.Any()
                ? JsonSerializer.Serialize(selection.AreaCodes)
                : "[]";

            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertySelectionEntity>("Property.PropertySelection_UpsertByUserId", new
            {
                UserId = selection.UserId,
                FromUnitLevel = selection.FromUnitLevel,
                ToUnitLevel = selection.ToUnitLevel,
                FromBeds = selection.FromBeds,
                ToBeds = selection.ToBeds,
                Accomodates = selection.Accomodates,
                MaxRent = selection.MaxRent,
                PropertyCode = selection.PropertyCode,
                PropertyLeaseTypeId = selection.PropertyLeaseTypeId,
                City = selection.City,
                State = selection.State,
                Cable = selection.Cable,
                Streaming = selection.Streaming,
                Pool = selection.Pool,
                Jacuzzi = selection.Jacuzzi,
                Security = selection.Security,
                Parking = selection.Parking,
                Pets = selection.Pets,
                Smoking = selection.Smoking,
                HighSpeedInternet = selection.HighSpeedInternet,
                PropertyStatusId = selection.PropertyStatusId,
                OfficeCode = selection.OfficeCode,
                BuildingCodes = buildingCodesJson,
                RegionCodes = regionCodesJson,
                AreaCodes = areaCodesJson
            });

            if (res == null || !res.Any())
                throw new Exception("Property selection not saved");

            return ConvertEntityToModel(res.First()!);
        }
        #endregion
    }
}
