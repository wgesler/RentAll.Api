using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Select
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

        #region Upsert
        public async Task<PropertySelection> UpsertPropertySelectionAsync(PropertySelection selection)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertySelectionEntity>("Property.PropertySelection_UpsertByUserId", new
            {
                UserId = selection.UserId,
                FromBeds = selection.FromBeds,
                ToBeds = selection.ToBeds,
                Accomodates = selection.Accomodates,
                MaxRent = selection.MaxRent,
                PropertyCode = selection.PropertyCode,
                City = selection.City,
                State = selection.State,
                Unfurnished = selection.Unfurnished,
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
                BuildingCode = selection.BuildingCode,
                RegionCode = selection.RegionCode,
                AreaCode = selection.AreaCode
            });

            if (res == null || !res.Any())
                throw new Exception("Property selection not saved");

            return ConvertEntityToModel(res.First()!);
        }
        #endregion
    }
}
