using RentAll.Domain.Models;
using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyExportItemDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public string? PrimaryPhotoUrl { get; set; }

    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;

    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accommodates { get; set; }
    public int SquareFeet { get; set; }
    public int PropertyStyleId { get; set; }
    public int PropertyTypeId { get; set; }

    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal? DepartureFee { get; set; }
    public decimal? MaidServiceFee { get; set; }
    public decimal? PetFee { get; set; }

    public List<string> ExternalCalendars { get; set; } = [];
    public string Description { get; set; } = string.Empty;

    public bool? IsActive { get; set; }
    public int? MinStay { get; set; }
    public int? MaxStay { get; set; }
    public int? CheckInTimeId { get; set; }
    public int? CheckOutTimeId { get; set; }
    public int? BedroomId1 { get; set; }
    public int? BedroomId2 { get; set; }
    public int? BedroomId3 { get; set; }
    public int? BedroomId4 { get; set; }

    public string? Neighborhood { get; set; }
    public string? CrossStreet { get; set; }
    public string? View { get; set; }
    public string? Mailbox { get; set; }

    public bool? Unfurnished { get; set; }
    public bool? Heating { get; set; }
    public bool? Ac { get; set; }
    public bool? Elevator { get; set; }
    public bool? Security { get; set; }
    public bool? Gated { get; set; }
    public bool? PetsAllowed { get; set; }
    public bool? DogsOkay { get; set; }
    public bool? CatsOkay { get; set; }
    public string? PoundLimit { get; set; }
    public bool? Smoking { get; set; }
    public bool? Parking { get; set; }
    public string? ParkingNotes { get; set; }

    public bool? Kitchen { get; set; }
    public bool? Oven { get; set; }
    public bool? Refrigerator { get; set; }
    public bool? Microwave { get; set; }
    public bool? Dishwasher { get; set; }
    public bool? Bathtub { get; set; }
    public bool? WasherDryerInUnit { get; set; }
    public bool? WasherDryerInBldg { get; set; }
    public bool? Tv { get; set; }
    public bool? Cable { get; set; }
    public bool? Dvd { get; set; }
    public bool? Streaming { get; set; }
    public bool? FastInternet { get; set; }
    public bool? Deck { get; set; }
    public bool? Patio { get; set; }
    public bool? Yard { get; set; }
    public bool? Garden { get; set; }
    public bool? CommonPool { get; set; }
    public bool? PrivatePool { get; set; }
    public bool? Jacuzzi { get; set; }
    public bool? Sauna { get; set; }
    public bool? Gym { get; set; }
    public string? Amenities { get; set; }

    public static ExternalPropertyExportItemDto FromExportList(ExternalExportPropertyList property)
    {
        return new ExternalPropertyExportItemDto
        {
            PropertyCode = property.PropertyCode,
            PropertyLeaseTypeId = property.PropertyLeaseTypeId,
            Address1 = property.Address1,
            Suite = property.Suite,
            City = property.City,
            State = property.State,
            Zip = property.Zip,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            Accommodates = property.Accommodates,
            SquareFeet = property.SquareFeet,
            PropertyStyleId = property.PropertyStyleId,
            PropertyTypeId = property.PropertyTypeId,
            MonthlyRate = property.MonthlyRate,
            DailyRate = property.DailyRate,
            DepartureFee = property.DepartureFee,
            MaidServiceFee = property.MaidServiceFee,
            PetFee = property.PetFee,
            ExternalCalendars = PropertyICalDto.Normalize(property.ExternalCalendars),
            Description = property.Description ?? string.Empty,
            Unfurnished = property.Unfurnished
        };
    }

    public static ExternalPropertyExportItemDto FromProperty(Property property)
    {
        return new ExternalPropertyExportItemDto
        {
            PropertyCode = property.PropertyCode,
            PropertyLeaseTypeId = Enum.IsDefined(typeof(PropertyLeaseType), property.PropertyLeaseType)
                ? (int)property.PropertyLeaseType
                : 0,
            Address1 = property.Address1,
            Address2 = property.Address2,
            Suite = property.Suite,
            City = property.City,
            State = property.State,
            Zip = property.Zip,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            Accommodates = property.Accommodates,
            SquareFeet = property.SquareFeet,
            PropertyStyleId = Enum.IsDefined(typeof(PropertyStyle), property.PropertyStyle)
                ? (int)property.PropertyStyle
                : 0,
            PropertyTypeId = Enum.IsDefined(typeof(PropertyType), property.PropertyType)
                ? (int)property.PropertyType
                : 0,
            MonthlyRate = property.MonthlyRate,
            DailyRate = property.DailyRate,
            DepartureFee = property.DepartureFee,
            MaidServiceFee = property.MaidServiceFee,
            PetFee = property.PetFee,
            ExternalCalendars = PropertyICalDto.Normalize(property.ExternalCalendars),
            Description = property.Description ?? string.Empty,
            IsActive = property.IsActive,
            MinStay = property.MinStay,
            MaxStay = property.MaxStay,
            CheckInTimeId = Enum.IsDefined(typeof(CheckInTime), property.CheckInTime)
                ? (int)property.CheckInTime
                : null,
            CheckOutTimeId = Enum.IsDefined(typeof(CheckOutTime), property.CheckOutTime)
                ? (int)property.CheckOutTime
                : null,
            BedroomId1 = property.BedroomId1,
            BedroomId2 = property.BedroomId2,
            BedroomId3 = property.BedroomId3,
            BedroomId4 = property.BedroomId4,
            Neighborhood = property.Neighborhood,
            CrossStreet = property.CrossStreet,
            View = property.View,
            Mailbox = property.Mailbox,
            Unfurnished = property.Unfurnished,
            Heating = property.Heating,
            Ac = property.Ac,
            Elevator = property.Elevator,
            Security = property.Security,
            Gated = property.Gated,
            PetsAllowed = property.PetsAllowed,
            DogsOkay = property.DogsOkay,
            CatsOkay = property.CatsOkay,
            PoundLimit = property.PoundLimit,
            Smoking = property.Smoking,
            Parking = property.Parking,
            ParkingNotes = property.ParkingNotes,
            Kitchen = property.Kitchen,
            Oven = property.Oven,
            Refrigerator = property.Refrigerator,
            Microwave = property.Microwave,
            Dishwasher = property.Dishwasher,
            Bathtub = property.Bathtub,
            WasherDryerInUnit = property.WasherDryerInUnit,
            WasherDryerInBldg = property.WasherDryerInBldg,
            Tv = property.Tv,
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
            Amenities = property.Amenities
        };
    }
}
