namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertySelectionResponseDto
{
    public Guid UserId { get; set; }
    public int FromUnitLevel { get; set; }
    public int ToUnitLevel { get; set; }
    public int FromBeds { get; set; }
    public int ToBeds { get; set; }
    public int Accomodates { get; set; }
    public decimal MaxRent { get; set; }
    public string? PropertyCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
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
    public List<string> BuildingCodes { get; set; } = new List<string>();
    public List<string> RegionCodes { get; set; } = new List<string>();
    public List<string> AreaCodes { get; set; } = new List<string>();

    public PropertySelectionResponseDto(PropertySelection s)
    {
        UserId = s.UserId;
        FromUnitLevel = s.FromUnitLevel;
        ToUnitLevel = s.ToUnitLevel;
        FromBeds = s.FromBeds;
        ToBeds = s.ToBeds;
        Accomodates = s.Accomodates;
        MaxRent = s.MaxRent;
        PropertyCode = s.PropertyCode;
        City = s.City;
        State = s.State;
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
        BuildingCodes = s.BuildingCodes;
        RegionCodes = s.RegionCodes;
        AreaCodes = s.AreaCodes;
    }
}


