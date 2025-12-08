using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Properties;
using RentAll.Infrastructure.Entities;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        private readonly string _dbConnectionString;

        public PropertyRepository(IOptions<AppSettings> appSettings)
        {
            _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
        }

        private Property ConvertEntityToModel(PropertyEntity e)
        {
            var response = new Property()
            {
                PropertyId = e.PropertyId,
                PropertyCode = e.PropertyCode,
                ContactId = e.ContactId,
                Name = e.Name,
                Address1 = e.Address1,
                Address2 = e.Address2,
                City = e.City,
                State = e.State,
                Zip = e.Zip,
                Phone = e.Phone,
                Bedrooms = e.Bedrooms,
                Bathrooms = e.Bathrooms,
                SquareFeet = e.SquareFeet,
                Gated = e.Gated,
                Alarm = e.Alarm,
                AlarmCode = e.AlarmCode,
                WasherDryer = e.WasherDryer,
                Amenities = e.Amenities,
                Pool = e.Pool,
                HotTub = e.HotTub,
                ParkingSpaces = e.ParkingSpaces,
                Yard = e.Yard,
                Amount = e.Amount,
                AmountTypeId = e.AmountTypeId,
                IsActive = e.IsActive,
                CreatedOn = e.CreatedOn,
                CreatedBy = e.CreatedBy,
                ModifiedOn = e.ModifiedOn,
                ModifiedBy = e.ModifiedBy
            };

            return response;
        }
    }
}