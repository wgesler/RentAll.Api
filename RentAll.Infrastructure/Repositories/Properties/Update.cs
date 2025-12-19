using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
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
				OrganizationId = property.OrganizationId,
				PropertyCode = property.PropertyCode,
				Owner1Id = property.Owner1Id,
				Owner2Id = property.Owner2Id,
				AvailableFrom = property.AvailableFrom,
				AvailableUntil = property.AvailableUntil,
				MinStay = property.MinStay,
				MaxStay = property.MaxStay,
				PropertyStyleId = (int)property.PropertyStyle,
				PropertyTypeId = (int)property.PropertyType,
				PropertyStatusId = (int)property.PropertyStatus,
				FranchiseCode = property.FranchiseCode,
				BuildingCode = property.BuildingCode,
				RegionCode = property.RegionCode,
				AreaCode = property.AreaCode,
				MonthlyRate = property.MonthlyRate,
				DailyRate = property.DailyRate,
				DepartureFee = property.DepartureFee,
				MaidServiceFee = property.MaidServiceFee,
				PetFee = property.PetFee,
				Bedrooms = property.Bedrooms,
				Bathrooms = property.Bathrooms,
				Accomodates = property.Accomodates,
				SquareFeet = property.SquareFeet,
				BedroomId1 = (int)property.Bedroom1,
				BedroomId2 = (int)property.Bedroom2,
				BedroomId3 = (int)property.Bedroom3,
				BedroomId4 = (int)property.Bedroom4,
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
				Parking = property.Parking,
				Notes = property.Notes,
				Alarm = property.Alarm,
				AlarmCode = property.AlarmCode,
				KeypadAccess = property.KeypadAccess,
				MasterKeyCode = property.MasterKeyCode,
				TenantKeyCode = property.TenantKeyCode,
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
				Streaming = property.Streaming,
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
				Description = property.Description,
				IsActive = property.IsActive,
				ModifiedBy = property.ModifiedBy
			});

            if (res == null || !res.Any())
                throw new Exception("Property not found");

            return ConvertEntityToModel(res.FirstOrDefault()!);
        }
    }
}
