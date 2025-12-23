using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties;

public class PropertyResponseDto
{
	public Guid PropertyId { get; set; }
	public Guid OrganizationId { get; set; }
	public string PropertyCode { get; set; } = string.Empty;
	public Guid Owner1Id { get; set; }
	public Guid? Owner2Id { get; set; }
	public Guid? Owner3Id { get; set; }

	// Availability Section 
	public DateTimeOffset? AvailableFrom { get; set; }
	public DateTimeOffset? AvailableUntil { get; set; }
	public int MinStay { get; set; }
	public int MaxStay { get; set; }
	public int CheckInTimeId { get; set; }
	public int CheckOutTimeId { get; set; }

	// Property Classification
	public int PropertyStyleId { get; set; }
	public int PropertyTypeId { get; set; }
	public int PropertyStatusId { get; set; }
	public string? FranchiseCode { get; set; }
	public string? BuildingCode { get; set; }
	public string? RegionCode { get; set; }
	public string? AreaCode { get; set; }

	// Rates & Fees
	public decimal MonthlyRate { get; set; }
	public decimal DailyRate { get; set; }
	public decimal DepartureFee { get; set; }
	public decimal MaidServiceFee { get; set; }
	public decimal PetFee { get; set; }
	public decimal ExtraFee { get; set; }
	public string ExtraFeeName { get; set; } = string.Empty;
	public int Bedrooms { get; set; }
	public decimal Bathrooms { get; set; }
	public int Accomodates { get; set; }
	public int SquareFeet { get; set; }
	public int BedroomId1 { get; set; }
	public int BedroomId2 { get; set; }
	public int BedroomId3 { get; set; }
	public int BedroomId4 { get; set; }

	// Address Section
	public string Address1 { get; set; } = string.Empty;
	public string? Address2 { get; set; }
	public string? Suite { get; set; }
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? Neighborhood { get; set; }
	public string? CrossStreet { get; set; }
	public string? View { get; set; }
	public string? Mailbox { get; set; }

	// Features & Security Section
	public bool Unfurnished { get; set; }
	public bool Heating { get; set; }
	public bool Ac { get; set; }
	public bool Elevator { get; set; }
	public bool Security { get; set; }
	public bool Gated { get; set; }
	public bool PetsAllowed { get; set; }
	public bool Smoking { get; set; }
	public bool Parking { get; set; }
	public string? ParkingNotes { get; set; }
	public bool Alarm { get; set; }
	public string? AlarmCode { get; set; }
	public bool KeypadAccess { get; set; }
	public string? MasterKeyCode { get; set; }
	public string? TenantKeyCode { get; set; }

	// Kitchen & Bath
	public bool Kitchen { get; set; }
	public bool Oven { get; set; }
	public bool Refrigerator { get; set; }
	public bool Microwave { get; set; }
	public bool Dishwasher { get; set; }
	public bool Bathtub { get; set; }
	public bool WasherDryer { get; set; }
	public bool Sofabeds { get; set; }

	// Electronics Section
	public bool Tv { get; set; }
	public bool Cable { get; set; }
	public bool Dvd { get; set; }
	public bool Streaming { get; set; }
	public bool FastInternet { get; set; }
	public string? InternetNetwork { get; set; }
	public string? InternetPassword { get; set; }

	//Outdoor Spaces Section
	public bool Deck { get; set; }
	public bool Patio { get; set; }
	public bool Yard { get; set; }
	public bool Garden { get; set; }

	// Pool & Spa Section
	public bool CommonPool { get; set; }
	public bool PrivatePool { get; set; }
	public bool Jacuzzi { get; set; }
	public bool Sauna { get; set; }
	public bool Gym { get; set; }

	// Trash Section
	public int TrashPickupId { get; set; }
	public string? TrashRemoval { get; set; }

	// Additional Amenities Section
	public string? Amenities { get; set; }
	public string? Description { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }


	public PropertyResponseDto(Property property)
	{
		PropertyId = property.PropertyId;
		OrganizationId = property.OrganizationId;
		PropertyCode = property.PropertyCode;
		Owner1Id = property.Owner1Id;
		Owner2Id = property.Owner2Id;
		Owner3Id = property.Owner3Id;
		AvailableFrom = property.AvailableFrom;
		AvailableUntil = property.AvailableUntil;
		MinStay = property.MinStay;
		MaxStay = property.MaxStay;
		CheckInTimeId = (int)property.CheckInTime;
		CheckOutTimeId = (int)property.CheckOutTime;
		PropertyStyleId = (int)property.PropertyStyle;
		PropertyTypeId = (int)property.PropertyType;
		PropertyStatusId = (int)property.PropertyStatus;
		FranchiseCode = property.FranchiseCode;
		BuildingCode = property.BuildingCode;
		RegionCode = property.RegionCode;
		AreaCode = property.AreaCode;
		MonthlyRate = property.MonthlyRate;
		DailyRate = property.DailyRate;
		DepartureFee = property.DepartureFee;
		MaidServiceFee = property.MaidServiceFee;
		PetFee = property.PetFee;
		ExtraFee = property.ExtraFee;
		ExtraFeeName = property.ExtraFeeName;
		Bedrooms = property.Bedrooms;
		Bathrooms = property.Bathrooms;
		Accomodates = property.Accomodates;
		SquareFeet = property.SquareFeet;
		BedroomId1 = (int)property.Bedroom1;
		BedroomId2 = (int)property.Bedroom2;
		BedroomId3 = (int)property.Bedroom3;
		BedroomId4 = (int)property.Bedroom4;
		Address1 = property.Address1;
		Address2 = property.Address2;
		Suite = property.Suite;
		City = property.City;
		State = property.State;
		Zip = property.Zip;
		Phone = property.Phone;
		Neighborhood = property.Neighborhood;
		CrossStreet = property.CrossStreet;
		View = property.View;
		Mailbox = property.Mailbox;
		Unfurnished = property.Unfurnished;
		Heating = property.Heating;
		Ac = property.Ac;
		Elevator = property.Elevator;
		Security = property.Security;
		Gated = property.Gated;
		PetsAllowed = property.PetsAllowed;
		Smoking = property.Smoking;
		Parking = property.Parking;
		ParkingNotes = property.ParkingNotes;
		Alarm = property.Alarm;
		AlarmCode = property.AlarmCode;
		KeypadAccess = property.KeypadAccess;
		MasterKeyCode = property.MasterKeyCode;
		TenantKeyCode = property.TenantKeyCode;
		Kitchen = property.Kitchen;
		Oven = property.Oven;
		Refrigerator = property.Refrigerator;
		Microwave = property.Microwave;
		Dishwasher = property.Dishwasher;
		Bathtub = property.Bathtub;
		WasherDryer = property.WasherDryer;
		Sofabeds = property.Sofabeds;
		Tv = property.Tv;
		Cable = property.Cable;
		Dvd = property.Dvd;
		Streaming = property.Streaming;
		FastInternet = property.FastInternet;
		InternetNetwork = property.InternetNetwork;
		InternetPassword = property.InternetPassword;
		Deck = property.Deck;
		Patio = property.Patio;
		Yard = property.Yard;
		Garden = property.Garden;
		CommonPool = property.CommonPool;
		PrivatePool = property.PrivatePool;
		Jacuzzi = property.Jacuzzi;
		Sauna = property.Sauna;
		Gym = property.Gym;
		TrashPickupId = property.TrashPickupId;
		TrashRemoval = property.TrashRemoval;
		Amenities = property.Amenities;
		Description = property.Description;
		Notes = property.Notes;
		IsActive = property.IsActive;
		CreatedOn = property.CreatedOn;
		CreatedBy = property.CreatedBy;
		ModifiedOn = property.ModifiedOn;
		ModifiedBy = property.ModifiedBy;
	}
}
