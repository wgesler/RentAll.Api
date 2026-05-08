namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertyListResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
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
    public int PropertyTypeId { get; set; }
    public bool Unfurnished { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public int PropertyStatusId { get; set; }
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }

    public Guid? onCleanerUserId { get; set; }
    public DateOnly? onCleaningDate { get; set; }
    public Guid? onCarpetUserId { get; set; }
    public DateOnly? onCarpetDate { get; set; }
    public Guid? onInspectorUserId { get; set; }
    public DateOnly? onInspectingDate { get; set; }

    public Guid? offCleanerUserId { get; set; }
    public DateOnly? offCleaningDate { get; set; }
    public Guid? offCarpetUserId { get; set; }
    public DateOnly? offCarpetDate { get; set; }
    public Guid? offInspectorUserId { get; set; }
    public DateOnly? offInspectingDate { get; set; }
    public bool OnlineChecked { get; set; }
    public bool OfflineChecked { get; set; }

    public bool IsActive { get; set; }

    public PropertyListResponseDto(PropertyList propertyList)
    {
        PropertyId = propertyList.PropertyId;
        PropertyCode = propertyList.PropertyCode;
        PropertyLeaseTypeId = (int)propertyList.PropertyLeaseType;
        ShortAddress = propertyList.ShortAddress;
        OfficeId = propertyList.OfficeId;
        OfficeName = propertyList.OfficeName;
        Owner1Id = propertyList.Owner1Id;
        VendorId = propertyList.VendorId;
        ContactName = propertyList.ContactName;
        AvailableFrom = propertyList.AvailableFrom;
        AvailableUntil = propertyList.AvailableUntil;
        UnitLevel = propertyList.UnitLevel;
        Bedrooms = propertyList.Bedrooms;
        Bathrooms = propertyList.Bathrooms;
        Accomodates = propertyList.Accomodates;
        SquareFeet = propertyList.SquareFeet;
        PropertyTypeId = (int)propertyList.PropertyType;
        Unfurnished = propertyList.Unfurnished;
        MonthlyRate = propertyList.MonthlyRate;
        DailyRate = propertyList.DailyRate;
        DepartureFee = propertyList.DepartureFee;
        PetFee = propertyList.PetFee;
        MaidServiceFee = propertyList.MaidServiceFee;
        PropertyStatusId = (int)propertyList.PropertyStatus;
        BedroomId1 = propertyList.BedroomId1;
        BedroomId2 = propertyList.BedroomId2;
        BedroomId3 = propertyList.BedroomId3;
        BedroomId4 = propertyList.BedroomId4;
        onCleanerUserId = propertyList.onCleanerUserId;
        onCleaningDate = propertyList.onCleaningDate;
        onCarpetUserId = propertyList.onCarpetUserId;
        onCarpetDate = propertyList.onCarpetDate;
        onInspectorUserId = propertyList.onInspectorUserId;
        onInspectingDate = propertyList.onInspectingDate;
        offCleanerUserId = propertyList.offCleanerUserId;
        offCleaningDate = propertyList.offCleaningDate;
        offCarpetUserId = propertyList.offCarpetUserId;
        offCarpetDate = propertyList.offCarpetDate;
        offInspectorUserId = propertyList.offInspectorUserId;
        offInspectingDate = propertyList.offInspectingDate;
        OnlineChecked = propertyList.OnlineChecked;
        OfflineChecked = propertyList.OfflineChecked;
        IsActive = propertyList.IsActive;
    }
}

