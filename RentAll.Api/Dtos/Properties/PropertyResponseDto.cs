using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties;

public class PropertyResponseDto
{
    public Guid PropertyId { get; set; }
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
    public decimal Amount { get; set; }
    public int AmountTypeId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public PropertyResponseDto(Property property)
    {
        PropertyId = property.PropertyId;
        PropertyCode = property.PropertyCode;
        ContactId = property.ContactId;
        Name = property.Name;
        Address1 = property.Address1;
        Address2 = property.Address2;
        City = property.City;
        State = property.State;
        Zip = property.Zip;
        Phone = property.Phone;
        Bedrooms = property.Bedrooms;
        Bathrooms = property.Bathrooms;
        SquareFeet = property.SquareFeet;
        Gated = property.Gated;
        Alarm = property.Alarm;
        AlarmCode = property.AlarmCode;
        WasherDryer = property.WasherDryer;
        Amenities = property.Amenities;
        Pool = property.Pool;
        HotTub = property.HotTub;
        ParkingSpaces = property.ParkingSpaces;
        Yard = property.Yard;
        Amount = property.Amount;
        AmountTypeId = property.AmountTypeId;
        IsActive = property.IsActive;
        CreatedOn = property.CreatedOn;
        CreatedBy = property.CreatedBy;
        ModifiedOn = property.ModifiedOn;
        ModifiedBy = property.ModifiedBy;
    }
}

