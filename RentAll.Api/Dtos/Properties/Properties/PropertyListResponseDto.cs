namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertyListResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public string ShortAddress { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid Owner1Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accomodates { get; set; }
    public int SquareFeet { get; set; }
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
        ShortAddress = propertyList.ShortAddress;
        OfficeId = propertyList.OfficeId;
        OfficeName = propertyList.OfficeName;
        Owner1Id = propertyList.Owner1Id;
        OwnerName = propertyList.OwnerName;
        AvailableFrom = propertyList.AvailableFrom;
        AvailableUntil = propertyList.AvailableUntil;
        Bedrooms = propertyList.Bedrooms;
        Bathrooms = propertyList.Bathrooms;
        Accomodates = propertyList.Accomodates;
        SquareFeet = propertyList.SquareFeet;
        MonthlyRate = propertyList.MonthlyRate;
        DailyRate = propertyList.DailyRate;
        DepartureFee = propertyList.DepartureFee;
        PetFee = propertyList.PetFee;
        MaidServiceFee = propertyList.MaidServiceFee;
        PropertyStatusId = (int)propertyList.PropertyStatus;
        IsActive = propertyList.IsActive;
    }
}

