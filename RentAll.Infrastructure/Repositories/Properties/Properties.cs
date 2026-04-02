using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository
    {
        #region Selects
        public async Task<IEnumerable<PropertyList>> GetPropertyListByOfficeIdsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetListByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<PropertyList>> GetPropertyActiveListByOfficeIdsAsync(Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetActiveListByOfficeIds", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<PropertyList>> GetPropertyListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetListBySelection", new
            {
                UserId = userId,
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<PropertyList>> GetActivePropertyListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetActiveListBySelection", new
            {
                UserId = userId,
                OrganizationId = organizationId,
                Offices = officeAccess
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<IEnumerable<PropertyList>> GetPropertyListByOwnerIdAsync(Guid ownerId, Guid organizationId, string officeAccess)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyListEntity>("Property.Property_GetListByOwnerId", new
            {
                OrganizationId = organizationId,
                Offices = officeAccess,
                OwnerId = ownerId
            });

            if (res == null || !res.Any())
                return Enumerable.Empty<PropertyList>();

            return res.Select(ConvertEntityToModel);
        }

        public async Task<Property?> GetPropertyByIdAsync(Guid propertyId, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("Property.Property_GetById", new
            {
                PropertyId = propertyId,
                OrganizationId = organizationId
            });

            if (res == null || !res.Any())
                return null;

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }

        public async Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var result = await db.DapperProcQueryScalarAsync<int>("Property.Property_ExistsByCode", new
            {
                PropertyCode = propertyCode,
                OrganizationId = organizationId
            });

            return result == 1;
        }
        #endregion

        #region Creates
        public async Task<Property> CreateAsync(Property property)
            {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("Property.Property_Add", new
            {
                OrganizationId = property.OrganizationId,
                PropertyCode = property.PropertyCode,
                Owner1Id = property.Owner1Id,
                Owner2Id = property.Owner2Id,
                Owner3Id = property.Owner3Id,
                AvailableFrom = property.AvailableFrom,
                AvailableUntil = property.AvailableUntil,
                MinStay = property.MinStay,
                MaxStay = property.MaxStay,
                CheckInTimeId = (int)property.CheckInTime,
                CheckOutTimeId = (int)property.CheckOutTime,
                PropertyStyleId = (int)property.PropertyStyle,
                PropertyTypeId = (int)property.PropertyType,
                PropertyStatusId = (int)property.PropertyStatus,
                OfficeId = property.OfficeId,
                BuildingId = property.BuildingId,
                RegionId = property.RegionId,
                AreaId = property.AreaId,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                MonthlyRate = property.MonthlyRate,
                DailyRate = property.DailyRate,
                DepartureFee = property.DepartureFee,
                MaidServiceFee = property.MaidServiceFee,
                PetFee = property.PetFee,
                ExtraFee = property.ExtraFee,
                ExtraFeeName = property.ExtraFeeName,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Accomodates = property.Accomodates,
                SquareFeet = property.SquareFeet,
                BedroomId1 = (int)property.Bedroom1,
                BedroomId2 = (int)property.Bedroom2,
                BedroomId3 = (int)property.Bedroom3,
                BedroomId4 = (int)property.Bedroom4,
                Sofabed = (int)property.Sofabed,
                Address1 = property.Address1,
                Address2 = property.Address2,
                Suite = property.Suite,
                City = property.City,
                State = property.State,
                Zip = property.Zip,
                Phone = property.Phone,
                Neighborhood = property.Neighborhood,
                CrossStreet = property.CrossStreet,
                View = property.View,
                Mailbox = property.Mailbox,
                Unfurnished = property.Unfurnished,
                Heating = property.Heating,
                AC = property.Ac,
                Elevator = property.Elevator,
                Security = property.Security,
                Gated = property.Gated,
                PetsAllowed = property.PetsAllowed,
                DogsOkay = property.DogsOkay,
                CatsOkay = property.CatsOkay,
                PoundLimit = property.PoundLimit,
                Smoking = property.Smoking,
                Parking = property.Parking,
                ParkingNotes = property.ParkingNotes,
                AlarmCode = property.AlarmCode,
                UnitMstrCode = property.UnitMstrCode,
                BldgMstrCode = property.BldgMstrCode,
                BldgTenantCode = property.BldgTenantCode,
                MailRoomCode = property.MailRoomCode,
                GarageCode = property.GarageCode,
                GateCode = property.GateCode,
                TrashCode = property.TrashCode,
                StorageCode = property.StorageCode,
                Kitchen = property.Kitchen,
                Oven = property.Oven,
                Refrigerator = property.Refrigerator,
                Microwave = property.Microwave,
                Dishwasher = property.Dishwasher,
                Bathtub = property.Bathtub,
                WasherDryerInUnit = property.WasherDryerInUnit,
                WasherDryerInBldg = property.WasherDryerInBldg,
                TV = property.Tv,
                Cable = property.Cable,
                Dvd = property.Dvd,
                Streaming = property.Streaming,
                FastInternet = property.FastInternet,
                InternetNetwork = property.InternetNetwork,
                InternetPassword = property.InternetPassword,
                Deck = property.Deck,
                Patio = property.Patio,
                Yard = property.Yard,
                Garden = property.Garden,
                CommonPool = property.CommonPool,
                PrivatePool = property.PrivatePool,
                Jacuzzi = property.Jacuzzi,
                Sauna = property.Sauna,
                Gym = property.Gym,
                TrashPickupId = property.TrashPickupId,
                TrashRemoval = property.TrashRemoval,
                Amenities = property.Amenities,
                Description = property.Description,
                Notes = property.Notes,
                IsActive = property.IsActive,
                CreatedBy = property.CreatedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Property not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Updates
        public async Task<Property> UpdateByIdAsync(Property property)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("Property.Property_UpdateById", new
            {
                PropertyId = property.PropertyId,
                OrganizationId = property.OrganizationId,
                PropertyCode = property.PropertyCode,
                Owner1Id = property.Owner1Id,
                Owner2Id = property.Owner2Id,
                Owner3Id = property.Owner3Id,
                AvailableFrom = property.AvailableFrom,
                AvailableUntil = property.AvailableUntil,
                MinStay = property.MinStay,
                MaxStay = property.MaxStay,
                CheckInTimeId = (int)property.CheckInTime,
                CheckOutTimeId = (int)property.CheckOutTime,
                PropertyStyleId = (int)property.PropertyStyle,
                PropertyTypeId = (int)property.PropertyType,
                PropertyStatusId = (int)property.PropertyStatus,
                OfficeId = property.OfficeId,
                BuildingId = property.BuildingId,
                RegionId = property.RegionId,
                AreaId = property.AreaId,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                MonthlyRate = property.MonthlyRate,
                DailyRate = property.DailyRate,
                DepartureFee = property.DepartureFee,
                MaidServiceFee = property.MaidServiceFee,
                PetFee = property.PetFee,
                ExtraFee = property.ExtraFee,
                ExtraFeeName = property.ExtraFeeName,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Accomodates = property.Accomodates,
                SquareFeet = property.SquareFeet,
                BedroomId1 = (int)property.Bedroom1,
                BedroomId2 = (int)property.Bedroom2,
                BedroomId3 = (int)property.Bedroom3,
                BedroomId4 = (int)property.Bedroom4,
                Sofabed = property.Sofabed,
                Address1 = property.Address1,
                Address2 = property.Address2,
                Suite = property.Suite,
                City = property.City,
                State = property.State,
                Zip = property.Zip,
                Phone = property.Phone,
                Neighborhood = property.Neighborhood,
                CrossStreet = property.CrossStreet,
                View = property.View,
                Mailbox = property.Mailbox,
                Unfurnished = property.Unfurnished,
                Heating = property.Heating,
                AC = property.Ac,
                Elevator = property.Elevator,
                Security = property.Security,
                Gated = property.Gated,
                PetsAllowed = property.PetsAllowed,
                DogsOkay = property.DogsOkay,
                CatsOkay = property.CatsOkay,
                PoundLimit = property.PoundLimit,
                Smoking = property.Smoking,
                Parking = property.Parking,
                ParkingNotes = property.ParkingNotes,
                AlarmCode = property.AlarmCode,
                UnitMstrCode = property.UnitMstrCode,
                BldgMstrCode = property.BldgMstrCode,
                BldgTenantCode = property.BldgTenantCode,
                MailRoomCode = property.MailRoomCode,
                GarageCode = property.GarageCode,
                GateCode = property.GateCode,
                TrashCode = property.TrashCode,
                StorageCode = property.StorageCode,
                Kitchen = property.Kitchen,
                Oven = property.Oven,
                Refrigerator = property.Refrigerator,
                Microwave = property.Microwave,
                Dishwasher = property.Dishwasher,
                Bathtub = property.Bathtub,
                WasherDryerInUnit = property.WasherDryerInUnit,
                WasherDryerInBldg = property.WasherDryerInBldg,
                TV = property.Tv,
                Cable = property.Cable,
                Dvd = property.Dvd,
                Streaming = property.Streaming,
                FastInternet = property.FastInternet,
                InternetNetwork = property.InternetNetwork,
                InternetPassword = property.InternetPassword,
                Deck = property.Deck,
                Patio = property.Patio,
                Yard = property.Yard,
                Garden = property.Garden,
                CommonPool = property.CommonPool,
                PrivatePool = property.PrivatePool,
                Jacuzzi = property.Jacuzzi,
                Sauna = property.Sauna,
                Gym = property.Gym,
                TrashPickupId = property.TrashPickupId,
                TrashRemoval = property.TrashRemoval,
                Amenities = property.Amenities,
                Description = property.Description,
                Notes = property.Notes,
                IsActive = property.IsActive,
                ModifiedBy = property.ModifiedBy
            });

            if (res == null || !res.Any())
                throw new Exception("Property not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
        #endregion

        #region Deletes
        public async Task DeletePropertyByIdAsync(Guid propertyId)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("Property.Property_DeleteById", new
            {
                PropertyId = propertyId
            });
        }
        #endregion
    }
}
