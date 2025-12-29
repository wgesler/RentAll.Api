using System.Data.SqlClient;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;

namespace RentAll.Infrastructure.Repositories.Properties
{
    public partial class PropertyRepository : IPropertyRepository
    {
        public async Task<Property> CreateAsync(Property property)
        {
            await using var db = new SqlConnection(_dbConnectionString);
			await db.DapperProcExecuteAsync("dbo.Property_Add", new
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
				FranchiseId = property.FranchiseId,
				BuildingId = property.BuildingId,
				RegionId = property.RegionId,
				AreaId = property.AreaId,
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
				Smoking = property.Smoking,
				Parking = property.Parking,
				ParkingNotes = property.ParkingNotes,
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

            // Retrieve the created property by PropertyCode
            var createdProperty = await GetByPropertyCodeAsync(property.PropertyCode, property.OrganizationId);
            if (createdProperty == null)
                throw new Exception("Property not created");

            return createdProperty;
        }
    }
}
