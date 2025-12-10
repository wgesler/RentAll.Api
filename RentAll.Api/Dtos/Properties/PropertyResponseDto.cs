using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties;

public class PropertyResponseDto
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
	public int CheckInTimeId { get; set; }
	public int CheckOutTimeId { get; set; }
	public decimal MonthlyRate { get; set; }
	public decimal DailyRate { get; set; }
	public int PropertyStyleId { get; set; }
	public int PropertyTypeId { get; set; }
	public int PropertyStatusId { get; set; }
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
	public string? Mailbox { get; set; }

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


    public PropertyResponseDto(Property property)
    {
        PropertyId = property.PropertyId;
        PropertyCode = property.PropertyCode;
        ContactId = property.ContactId;
        IsActive = property.IsActive;
        AvailableFrom = property.AvailableFrom;
        AvailableUntil = property.AvailableUntil;
        MinStay = property.MinStay;
        MaxStay = property.MaxStay;
        CheckInTimeId = (int)property.CheckInTime;
        CheckOutTimeId = (int)property.CheckOutTime;
        MonthlyRate = property.MonthlyRate;
        DailyRate = property.DailyRate;
        PropertyStyleId = (int)property.PropertyStyle;
        PropertyTypeId = (int)property.PropertyType;
        PropertyStatusId = (int)property.PropertyStatus;
        Bedrooms = property.Bedrooms;
        Bathrooms = property.Bathrooms;
        Accomodates = property.Accomodates;
        SquareFeet = property.SquareFeet;
        BedSizes = property.BedSizes;
        Address1 = property.Address1;
        Address2 = property.Address2;
        Suite = property.Suite ?? string.Empty;
        City = property.City;
        State = property.State;
        Zip = property.Zip;
        Phone = property.Phone;
        Neighborhood = property.Neighborhood;
        CrossStreet = property.CrossStreet;
        View = property.View;
        Mailbox = property.Mailbox;
        Furnished = property.Furnished;
        Heating = property.Heating;
        Ac = property.Ac;
        Elevator = property.Elevator;
        Security = property.Security;
        Gated = property.Gated;
        PetsAllowed = property.PetsAllowed;
        Smoking = property.Smoking;
        AssignedParking = property.AssignedParking;
        Notes = property.Notes;
        Alarm = property.Alarm;
        AlarmCode = property.AlarmCode;
        RemoteAccess = property.RemoteAccess;
        KeyCode = property.KeyCode;
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
        FastInternet = property.FastInternet;
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
    }
}
