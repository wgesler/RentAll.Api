using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties;

public class CreatePropertyDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "Property Code is required");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

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

    public Property ToModel(CreatePropertyDto p, Guid currentUser)
    {
        return new Property
        {
            PropertyCode = p.PropertyCode,
            ContactId = p.ContactId,
            Name = p.Name,
            Address1 = p.Address1,
            Address2 = p.Address2,
            City = p.City,
            State = p.State,
            Zip = p.Zip,
            Phone = p.Phone,
            Bedrooms = p.Bedrooms,
            Bathrooms = p.Bathrooms,
            SquareFeet = p.SquareFeet,
            Gated = p.Gated,
            Alarm = p.Alarm,
            AlarmCode = p.AlarmCode,
            WasherDryer = p.WasherDryer,
            Amenities = p.Amenities,
            Pool = p.Pool,
            HotTub = p.HotTub,
            ParkingSpaces = p.ParkingSpaces,
            Yard = p.Yard,
            IsActive = true,
            CreatedBy = currentUser
        };
    }
}