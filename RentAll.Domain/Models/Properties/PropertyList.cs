using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class PropertyList
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public PropertyLeaseType PropertyLeaseType { get; set; }
    public string ShortAddress { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? Owner1Id { get; set; }
    public Guid? VendorId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
    public int UnitLevel { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accomodates { get; set; }
    public int SquareFeet { get; set; }
    public PropertyType PropertyType { get; set; }
    public bool Unfurnished { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public PropertyStatus PropertyStatus { get; set; }
    public bool IsActive { get; set; }
}

