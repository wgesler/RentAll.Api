using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Property
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
	public CheckInTime CheckInTime { get; set; }
	public CheckOutTime CheckOutTime { get; set; }

	// Property Classification
	public PropertyStyle PropertyStyle { get; set; }
	public PropertyType PropertyType { get; set; }
	public PropertyStatus PropertyStatus { get; set; }
	public int OfficeId { get; set; }
	public int? BuildingId { get; set; }
	public int? RegionId { get; set; }
	public int? AreaId { get; set; }

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
	public BedSizeType Bedroom1 { get; set; }
	public BedSizeType Bedroom2 { get; set; }
	public BedSizeType Bedroom3 { get; set; }
	public BedSizeType Bedroom4 { get; set; }

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
}
