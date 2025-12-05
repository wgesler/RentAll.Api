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

        private Property ConvertDtoToModel(PropertyEntity dto)
        {
            var response = new Property()
            {
                PropertyId = dto.PropertyId,
                PropertyCode = dto.PropertyCode,
                Owner = dto.Owner,
                Address1 = dto.Address1,
                Address2 = dto.Address2,
                City = dto.City,
                State = dto.State,
                Zip = dto.Zip,
                Phone = dto.Phone,
                Bedrooms = dto.Bedrooms,
                Bathrooms = dto.Bathrooms,
                SquareFeet = dto.SquareFeet,
                Gated = dto.Gated,
                Alarm = dto.Alarm,
                AlarmCode = dto.AlarmCode,
                WasherDryer = dto.WasherDryer,
                Amenities = dto.Amenities,
                Pool = dto.Pool,
                HotTub = dto.HotTub,
                ParkingSpaces = dto.ParkingSpaces,
                Yard = dto.Yard,
                Amount = dto.Amount,
                AmountTypeId = dto.AmountTypeId,
                IsActive = dto.IsActive,
                CreatedOn = dto.CreatedOn,
                CreatedBy = dto.CreatedBy,
                ModifiedOn = dto.ModifiedOn,
                ModifiedBy = dto.ModifiedBy
            };

            return response;
        }
    }
}