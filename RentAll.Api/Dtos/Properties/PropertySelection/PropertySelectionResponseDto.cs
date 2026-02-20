using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertySelectionResponseDto
{
    public Guid UserId { get; set; }
    public int FromBeds { get; set; }
    public int ToBeds { get; set; }
    public int Accomodates { get; set; }
    public decimal MaxRent { get; set; }
    public string? PropertyCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool Unfurnished { get; set; }
    public bool Cable { get; set; }
    public bool Streaming { get; set; }
    public bool Pool { get; set; }
    public bool Jacuzzi { get; set; }
    public bool Security { get; set; }
    public bool Parking { get; set; }
    public bool Pets { get; set; }
    public bool Smoking { get; set; }
    public bool HighSpeedInternet { get; set; }
    public int PropertyStatusId { get; set; }
    public string? OfficeCode { get; set; }
    public string? BuildingCode { get; set; }
    public string? RegionCode { get; set; }
    public string? AreaCode { get; set; }

    public PropertySelectionResponseDto(PropertySelection s)
    {
        UserId = s.UserId;
        FromBeds = s.FromBeds;
        ToBeds = s.ToBeds;
        Accomodates = s.Accomodates;
        MaxRent = s.MaxRent;
        PropertyCode = s.PropertyCode;
        City = s.City;
        State = s.State;
        Unfurnished = s.Unfurnished;
        Cable = s.Cable;
        Streaming = s.Streaming;
        Pool = s.Pool;
        Jacuzzi = s.Jacuzzi;
        Security = s.Security;
        Parking = s.Parking;
        Pets = s.Pets;
        Smoking = s.Smoking;
        HighSpeedInternet = s.HighSpeedInternet;
        PropertyStatusId = s.PropertyStatusId;
        OfficeCode = s.OfficeCode;
        BuildingCode = s.BuildingCode;
        RegionCode = s.RegionCode;
        AreaCode = s.AreaCode;
    }
}


