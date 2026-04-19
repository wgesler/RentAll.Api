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
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
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
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }

    // Online Service Providers
    public Guid? OnlineCleanerUserId { get; set; }
    public DateOnly? OnlineCleaningDate { get; set; }
    public Guid? OnlineCarpetUserId { get; set; }
    public DateOnly? OnlineCarpetDate { get; set; }
    public Guid? OnlineInspectorUserId { get; set; }
    public DateOnly? OnlineInspectingDate { get; set; }

    // Offline Service Providers
    public Guid? OfflineCleanerUserId { get; set; }
    public DateOnly? OfflineCleaningDate { get; set; }
    public Guid? OfflineCarpetUserId { get; set; }
    public DateOnly? OfflineCarpetDate { get; set; }
    public Guid? OfflineInspectorUserId { get; set; }
    public DateOnly? OfflineInspectingDate { get; set; }

    public bool IsActive { get; set; }
}

