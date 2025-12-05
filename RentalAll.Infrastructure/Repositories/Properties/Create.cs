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

