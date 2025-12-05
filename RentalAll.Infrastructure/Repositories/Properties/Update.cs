using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        public async Task<Property> UpdateByIdAsync(Property property)
        {
            await using var db = new SqlConnection(_dbConnectionString);
            var res = await db.DapperProcQueryAsync<PropertyEntity>("dbo.Property_UpdateById", new
            {
                PropertyId = property.PropertyId,
                PropertyCode = property.PropertyCode,
                Owner = property.Owner,
                Address1 = property.Address1,
                Address2 = property.Address2,
                City = property.City,
                State = property.State,
                Zip = property.Zip,
                Phone = property.Phone,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                SquareFeet = property.SquareFeet,
                Gated = property.Gated,
                Alarm = property.Alarm,
                AlarmCode = property.AlarmCode,
                WasherDryer = property.WasherDryer,
                Amenities = property.Amenities,
                Pool = property.Pool,
                HotTub = property.HotTub,
                ParkingSpaces = property.ParkingSpaces,
                Yard = property.Yard,
                Amount = property.Amount,
                AmountTypeId = property.AmountTypeId,
                IsActive = property.IsActive
            });

            if (res == null || !res.Any())
                throw new Exception("Property not found");

            return ConvertDtoToModel(res.FirstOrDefault()!);
        }
    }
}

