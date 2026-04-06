namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertyListResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseId { get; set; }
    public string ShortAddress { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid? Owner1Id { get; set; }
    public Guid? VendorId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accomodates { get; set; }
    public int SquareFeet { get; set; }
    public int PropertyTypeId { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public int PropertyStatusId { get; set; }
    public bool IsActive { get; set; }

    public PropertyListResponseDto(PropertyList propertyList)
    {
        PropertyId = propertyList.PropertyId;
        PropertyCode = propertyList.PropertyCode;
        PropertyLeaseId = (int)propertyList.PropertyLeaseType;
        ShortAddress = propertyList.ShortAddress;
        OfficeId = propertyList.OfficeId;
        OfficeName = propertyList.OfficeName;
        Owner1Id = propertyList.Owner1Id;
        VendorId = propertyList.VendorId;
        ContactName = propertyList.ContactName;
        AvailableFrom = propertyList.AvailableFrom;
        AvailableUntil = propertyList.AvailableUntil;
        Bedrooms = propertyList.Bedrooms;
        Bathrooms = propertyList.Bathrooms;
        Accomodates = propertyList.Accomodates;
        SquareFeet = propertyList.SquareFeet;
        PropertyTypeId = (int)propertyList.PropertyType;
        MonthlyRate = propertyList.MonthlyRate;
        DailyRate = propertyList.DailyRate;
        DepartureFee = propertyList.DepartureFee;
        PetFee = propertyList.PetFee;
        MaidServiceFee = propertyList.MaidServiceFee;
        PropertyStatusId = (int)propertyList.PropertyStatus;
        IsActive = propertyList.IsActive;
    }
}

