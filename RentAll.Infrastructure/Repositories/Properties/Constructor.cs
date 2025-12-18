using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Enums;
using RentAll.Infrastructure.Entities;
using RentAll.Domain.Models;

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
				OrganizationId = e.OrganizationId,
				PropertyCode = e.PropertyCode,
				Owner1Id = e.Owner1Id,
				Owner2Id = e.Owner2Id,
				AvailableFrom = e.AvailableFrom,
				AvailableUntil = e.AvailableUntil,
				MinStay = e.MinStay,
				MaxStay = e.MaxStay,
				PropertyStyle = (PropertyStyle)e.PropertyStyleId,
				PropertyType = (PropertyType)e.PropertyTypeId,
				PropertyStatus = (PropertyStatus)e.PropertyStatusId,
				MonthlyRate = e.MonthlyRate,
				DailyRate = e.DailyRate,
				DepartureFee = e.DepartureFee,
				MaidServiceFee = e.MaidServiceFee,
				PetFee = e.PetFee,
				Bedrooms = e.Bedrooms,
				Bathrooms = e.Bathrooms,
				Accomodates = e.Accomodates,
				SquareFeet = e.SquareFeet,
				Bedroom1 = (BedSizeType)e.BedroomId1,
				Bedroom2 = (BedSizeType)e.BedroomId2,
				Bedroom3 = (BedSizeType)e.BedroomId3,
				Bedroom4 = (BedSizeType)e.BedroomId4,
				Address1 = e.Address1,
				Address2 = e.Address2,
				Suite = e.Suite,
				City = e.City,
				State = e.State,
				Zip = e.Zip,
				Phone = e.Phone,
				Neighborhood = e.Neighborhood,
				CrossStreet = e.CrossStreet,
				View = e.View,
				Mailbox = e.Mailbox,
				Furnished = e.Furnished,
				Heating = e.Heating,
				Ac = e.Ac,
				Elevator = e.Elevator,
				Security = e.Security,
				Gated = e.Gated,
				PetsAllowed = e.PetsAllowed,
				Smoking = e.Smoking,
				AssignedParking = e.AssignedParking,
				Notes = e.Notes,
				Alarm = e.Alarm,
				AlarmCode = e.AlarmCode,
				KeypadAccess = e.KeypadAccess,
				MasterKeyCode = e.MasterKeyCode,
				TenantKeyCode = e.TenantKeyCode,
				Kitchen = e.Kitchen,
				Oven = e.Oven,
				Refrigerator = e.Refrigerator,
				Microwave = e.Microwave,
				Dishwasher = e.Dishwasher,
				Bathtub = e.Bathtub,
				WasherDryer = e.WasherDryer,
				Sofabeds = e.Sofabeds,
				Tv = e.Tv,
				Cable = e.Cable,
				Dvd = e.Dvd,
				Streaming = e.Streaming,
				FastInternet = e.FastInternet,
				Deck = e.Deck,
				Patio = e.Patio,
				Yard = e.Yard,
				Garden = e.Garden,
				CommonPool = e.CommonPool,
				PrivatePool = e.PrivatePool,
				Jacuzzi = e.Jacuzzi,
				Sauna = e.Sauna,
				Gym = e.Gym,
				TrashPickupId = e.TrashPickupId,
				TrashRemoval = e.TrashRemoval,
				Amenities = e.Amenities,
				Description = e.Description,
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
