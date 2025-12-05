using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties;

public class UpdatePropertyDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid Owner { get; set; }
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int SquareFeet { get; set; }
    public bool Gated { get; set; }
    public bool Alarm { get; set; }
    public string AlarmCode { get; set; } = string.Empty;
    public bool WasherDryer { get; set; }
    public string Amenities { get; set; } = string.Empty;
    public bool Pool { get; set; }
    public bool HotTub { get; set; }
    public int ParkingSpaces { get; set; }
    public bool Yard { get; set; }
    public decimal Amount { get; set; }
    public int AmountTypeId { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "Property ID is required");

        if (PropertyId != id)
            return (false, "Property ID mismatch");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "Property Code is required");

        if (Owner == Guid.Empty)
            return (false, "Owner is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        if (string.IsNullOrWhiteSpace(Phone))
            return (false, "Phone is required");

        return (true, null);
    }

    public Property ToModel(Property existingProperty, Guid currentUser)
    {
        return new Property
        {
            PropertyId = PropertyId,
            PropertyCode = PropertyCode,
            Owner = Owner,
            Address1 = Address1,
            Address2 = Address2,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Bedrooms = Bedrooms,
            Bathrooms = Bathrooms,
            SquareFeet = SquareFeet,
            Gated = Gated,
            Alarm = Alarm,
            AlarmCode = AlarmCode,
            WasherDryer = WasherDryer,
            Amenities = Amenities,
            Pool = Pool,
            HotTub = HotTub,
            ParkingSpaces = ParkingSpaces,
            Yard = Yard,
            Amount = Amount,
            AmountTypeId = AmountTypeId,
            IsActive = IsActive,
            CreatedOn = existingProperty.CreatedOn,
            CreatedBy = existingProperty.CreatedBy,
            ModifiedBy = currentUser
        };
    }
}

