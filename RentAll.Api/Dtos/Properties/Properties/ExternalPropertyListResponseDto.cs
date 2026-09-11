using RentAll.Domain.Models.Properties;

namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyListResponseDto
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public string Address1 { get; set; } = string.Empty;
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int UnitLevel { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accommodates { get; set; }
    public int SquareFeet { get; set; }
    public int PropertyTypeId { get; set; }
    public int PropertyStyleId { get; set; }
    public bool Unfurnished { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public int PropertyStatusId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalCalendar { get; set; }
    public string? Description { get; set; }

    public ExternalPropertyListResponseDto(ExternalExportPropertyList property)
    {
        PropertyId = property.PropertyId;
        OrganizationId = property.OrganizationId;
        PropertyCode = property.PropertyCode;
        PropertyLeaseTypeId = property.PropertyLeaseTypeId;
        Address1 = property.Address1;
        Suite = property.Suite;
        City = property.City;
        State = property.State;
        Zip = property.Zip;
        OfficeId = property.OfficeId;
        OfficeName = property.OfficeName;
        VendorId = property.VendorId;
        AvailableFrom = property.AvailableFrom;
        AvailableUntil = property.AvailableUntil;
        UnitLevel = property.UnitLevel;
        Bedrooms = property.Bedrooms;
        Bathrooms = property.Bathrooms;
        Accommodates = property.Accommodates;
        SquareFeet = property.SquareFeet;
        PropertyTypeId = property.PropertyTypeId;
        PropertyStyleId = property.PropertyStyleId;
        Unfurnished = property.Unfurnished;
        MonthlyRate = property.MonthlyRate;
        DailyRate = property.DailyRate;
        DepartureFee = property.DepartureFee;
        PetFee = property.PetFee;
        MaidServiceFee = property.MaidServiceFee;
        PropertyStatusId = property.PropertyStatusId;
        Latitude = property.Latitude;
        Longitude = property.Longitude;
        ExternalCalendar = property.ExternalCalendar;
        Description = property.Description;
    }
}
