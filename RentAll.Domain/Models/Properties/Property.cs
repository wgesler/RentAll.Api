using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Properties;

public class Property
{
    public Guid PropertyId { get; set; }
	public string PropertyCode { get; set; } = string.Empty;
	public Guid ContactId { get; set; }
	public bool IsActive { get; set; }


	// Availability Section 
	public DateTimeOffset? AvailableFrom { get; set; }
	public DateTimeOffset? AvailableUntil { get; set; }
	public int MinStay { get; set; }
	public int MaxStay { get; set; }
	public CheckInTime CheckInTime { get; set; }
	public CheckOutTime CheckOutTime { get; set; }
	public decimal MonthlyRate { get; set; }
	public decimal DailyRate { get; set; }
	public PropertyStyle PropertyStyle { get; set; }
	public PropertyType PropertyType { get; set; }
	public PropertyStatus PropertyStatus { get; set; }
	public int Bedrooms { get; set; }
	public decimal Bathrooms { get; set; }
	public int Accomodates { get; set; }
	public int SquareFeet { get; set; }
	public string BedSizes { get; set; } = string.Empty;

	// Address Section
	public string Address1 { get; set; } = string.Empty;
	public string Address2 { get; set; } = string.Empty;
	public string Suite { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string Phone { get; set; } = string.Empty;
	public string Neighborhood { get; set; } = string.Empty;
	public string CrossStreet { get; set; } = string.Empty;
	public string View { get; set; } = string.Empty;
	public string Mailbox { get; set; } = string.Empty;

	// Featues & Security Section
	public bool Furnished { get; set; }
	public bool Heating { get; set; }
	public bool Ac { get; set; }
	public bool Elevator { get; set; }
	public bool Security { get; set; }
	public bool Gated { get; set; }
	public bool PetsAllowed { get; set; }
	public bool Smoking { get; set; }
	public bool AssignedParking { get; set; }
	public string Notes { get; set; } = string.Empty;
	public bool Alarm { get; set; }
	public string AlarmCode { get; set; } = string.Empty;
	public bool RemoteAccess { get; set; }
	public string KeyCode { get; set; } = string.Empty;

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
	public bool FastInternet { get; set; }

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
	public string TrashRemoval { get; set; } = string.Empty;

	// Additional Amenities Section
	public string Amenities { get; set; } = string.Empty;

	public bool IsDeleted { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}
