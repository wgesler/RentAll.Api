using RentAll.Domain.Models.Properties;
using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Properties;

public class CreatePropertyDto
{
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
	public string? Address2 { get; set; }
	public string Suite { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? Neighborhood { get; set; }
	public string? CrossStreet { get; set; }
	public string? View { get; set; }
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
	public string? Notes { get; set; }
	public bool Alarm { get; set; }
	public string? AlarmCode { get; set; }
	public bool RemoteAccess { get; set; }
	public string? KeyCode { get; set; }

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
	public string? Amenities { get; set; }


	public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "Property Code is required");
		
        if (ContactId == Guid.Empty)
			return (false, "Contact ID is required");

		if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        // Validate enum values
        if (!Enum.IsDefined(typeof(PropertyStyle), PropertyStyleId))
            return (false, $"Invalid PropertyStyle value: {PropertyStyleId}");

        if (!Enum.IsDefined(typeof(PropertyType), PropertyTypeId))
            return (false, $"Invalid PropertyType value: {PropertyTypeId}");

        if (!Enum.IsDefined(typeof(PropertyStatus), PropertyStatusId))
            return (false, $"Invalid PropertyStatus value: {PropertyStatusId}");

        if (!Enum.IsDefined(typeof(CheckInTime), CheckInTimeId))
            return (false, $"Invalid CheckInTime value: {CheckInTimeId}");

        if (!Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId))
            return (false, $"Invalid CheckOutTime value: {CheckOutTimeId}");

        return (true, null);
    }

    public Property ToModel(CreatePropertyDto p, Guid currentUser)
    {
        return new Property
        {
            PropertyCode = p.PropertyCode,
            ContactId = p.ContactId,
            IsActive = p.IsActive,
            AvailableFrom = p.AvailableFrom,
            AvailableUntil = p.AvailableUntil,
            MinStay = p.MinStay,
            MaxStay = p.MaxStay,
            CheckInTime = (CheckInTime)p.CheckInTimeId,
            CheckOutTime = (CheckOutTime)p.CheckOutTimeId,
            MonthlyRate = p.MonthlyRate,
            DailyRate = p.DailyRate,
            PropertyStyle = (PropertyStyle)p.PropertyStyleId,
            PropertyType = (PropertyType)p.PropertyTypeId,
            PropertyStatus = (PropertyStatus)p.PropertyStatusId,
            Bedrooms = p.Bedrooms,
            Bathrooms = p.Bathrooms,
            Accomodates = p.Accomodates,
            SquareFeet = p.SquareFeet,
            BedSizes = p.BedSizes ?? string.Empty,
            Address1 = p.Address1,
            Address2 = p.Address2 ?? string.Empty,
            Suite = p.Suite,
            City = p.City,
            State = p.State,
            Zip = p.Zip,
            Phone = p.Phone ?? string.Empty,
            Neighborhood = p.Neighborhood ?? string.Empty,
            CrossStreet = p.CrossStreet ?? string.Empty,
            View = p.View ?? string.Empty,
            Mailbox = p.Mailbox ?? string.Empty,
            Furnished = p.Furnished,
            Heating = p.Heating,
            Ac = p.Ac,
            Elevator = p.Elevator,
            Security = p.Security,
            Gated = p.Gated,
            PetsAllowed = p.PetsAllowed,
            Smoking = p.Smoking,
            AssignedParking = p.AssignedParking,
            Notes = p.Notes ?? string.Empty,
            Alarm = p.Alarm,
            AlarmCode = p.AlarmCode ?? string.Empty,
            RemoteAccess = p.RemoteAccess,
            KeyCode = p.KeyCode ?? string.Empty,
            Kitchen = p.Kitchen,
            Oven = p.Oven,
            Refrigerator = p.Refrigerator,
            Microwave = p.Microwave,
            Dishwasher = p.Dishwasher,
            Bathtub = p.Bathtub,
            WasherDryer = p.WasherDryer,
            Sofabeds = p.Sofabeds,
            Tv = p.Tv,
            Cable = p.Cable,
            Dvd = p.Dvd,
            FastInternet = p.FastInternet,
            Deck = p.Deck,
            Patio = p.Patio,
            Yard = p.Yard,
            Garden = p.Garden,
            CommonPool = p.CommonPool,
            PrivatePool = p.PrivatePool,
            Jacuzzi = p.Jacuzzi,
            Sauna = p.Sauna,
            Gym = p.Gym,
            TrashPickupId = p.TrashPickupId,
            TrashRemoval = p.TrashRemoval ?? string.Empty,
            Amenities = p.Amenities ?? string.Empty,
            CreatedBy = currentUser
        };
    }
}
