namespace RentAll.Domain.Models;

public class PropertySelection
{
    public Guid UserId { get; set; }
    public int FromUnitLevel { get; set; }
    public int ToUnitLevel { get; set; }
    public int FromBeds { get; set; }
    public int ToBeds { get; set; }
    public int Accomodates { get; set; }
    public decimal MaxRent { get; set; }
    public string? PropertyCode { get; set; }
    public int PropertyLeaseTypeId { get; set; }
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
}


