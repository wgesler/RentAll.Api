using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        public async Task<Property> CreateAsync(Property property)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            await db.DapperProcExecuteAsync("dbo.Property_Add", new
            {
                PropertyCode = property.PropertyCode,
                ContactId = property.ContactId,
                AvailableFrom = property.AvailableFrom,
                AvailableUntil = property.AvailableUntil,
                MinStay = property.MinStay,
                MaxStay = property.MaxStay,
                CheckInTimeId = (int)property.CheckInTime,
                CheckOutTimeId = (int)property.CheckOutTime,
                MonthlyRate = property.MonthlyRate,
                DailyRate = property.DailyRate,
                PropertyStyleId = (int)property.PropertyStyle,
                PropertyTypeId = (int)property.PropertyType,
                PropertyStatusId = (int)property.PropertyStatus,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Accomodates = property.Accomodates,
                SquareFeet = property.SquareFeet,
                BedSizes = property.BedSizes,
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
                Furnished = property.Furnished,
                Heating = property.Heating,
                AC = property.Ac,
                Elevator = property.Elevator,
                Security = property.Security,
                Gated = property.Gated,
                PetsAllowed = property.PetsAllowed,
                Smoking = property.Smoking,
                AssignedParking = property.AssignedParking,
                Notes = property.Notes,
                Alarm = property.Alarm,
                AlarmCode = property.AlarmCode,
                RemoteAccess = property.RemoteAccess,
                KeyCode = property.KeyCode,
                Kitchen = property.Kitchen,
                Oven = property.Oven,
                Refrigerator = property.Refrigerator,
                Microwave = property.Microwave,
                Dishwasher = property.Dishwasher,
                Bathtub = property.Bathtub,
                WasherDryer = property.WasherDryer,
                Sofabeds = property.Sofabeds,
                TV = property.Tv,
                Cable = property.Cable,
                Dvd = property.Dvd,
                FastInternet = property.FastInternet,
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
				CreatedBy = property.CreatedBy
            });

            // Retrieve the created property by PropertyCode
            var createdProperty = await GetByPropertyCodeAsync(property.PropertyCode);
            if (createdProperty == null)
                throw new Exception("Property not created");

            return createdProperty;
        }
    }
}
