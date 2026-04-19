namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertyResponseDto
{
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public Guid? Owner1Id { get; set; }
    public Guid? Owner2Id { get; set; }
    public Guid? Owner3Id { get; set; }
    public Guid? VendorId { get; set; }

    // Availability Section 
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int MinStay { get; set; }
    public int MaxStay { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }

    // Property Classification
    public int PropertyStyleId { get; set; }
    public int PropertyTypeId { get; set; }
    public int PropertyStatusId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public int? BuildingId { get; set; }
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Rates & Fees
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal ExtraFee { get; set; }
    public string ExtraFeeName { get; set; } = string.Empty;
    public string? BldgNo { get; set; }
    public int UnitLevel { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accomodates { get; set; }
    public int SquareFeet { get; set; }
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }
    public int Sofabed { get; set; }

    // Address Section
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CommunityAddress { get; set; }
    public string? Neighborhood { get; set; }
    public string? CrossStreet { get; set; }
    public string? View { get; set; }
    public string? Mailbox { get; set; }

    // Features & Security Section
    public bool Unfurnished { get; set; }
    public bool Heating { get; set; }
    public bool Ac { get; set; }
    public bool Elevator { get; set; }
    public bool Security { get; set; }
    public bool Gated { get; set; }
    public bool PetsAllowed { get; set; }
    public bool DogsOkay { get; set; }
    public bool CatsOkay { get; set; }
    public string PoundLimit { get; set; } = string.Empty;
    public bool Smoking { get; set; }
    public bool Parking { get; set; }
    public string? ParkingNotes { get; set; }
    public string? AlarmCode { get; set; }
    public string? UnitMstrCode { get; set; }
    public string? BldgMstrCode { get; set; }
    public string? BldgTenantCode { get; set; }
    public string? MailRoomCode { get; set; }
    public string? GarageCode { get; set; }
    public string? GateCode { get; set; }
    public string? TrashCode { get; set; }
    public string? StorageCode { get; set; }

    // Kitchen & Bath
    public bool Kitchen { get; set; }
    public bool Oven { get; set; }
    public bool Refrigerator { get; set; }
    public bool Microwave { get; set; }
    public bool Dishwasher { get; set; }
    public bool Bathtub { get; set; }
    public bool WasherDryerInUnit { get; set; }
    public bool WasherDryerInBldg { get; set; }

    // Electronics Section
    public bool Tv { get; set; }
    public bool Cable { get; set; }
    public bool Dvd { get; set; }
    public bool Streaming { get; set; }
    public bool FastInternet { get; set; }
    public string? InternetNetwork { get; set; }
    public string? InternetPassword { get; set; }

    //Outdoor Spaces Section
    public bool Deck { get; set; }
    public bool Patio { get; set; }
    public bool Yard { get; set; }
    public bool Garden { get; set; }

    // Pool & Spa Section
    public bool CommonPool { get; set; }
    public bool PrivatePool { get; set; }
    public bool Jacuzzi { get; set; }
    public bool Sauna { get; set; }
    public bool Gym { get; set; }

    // Trash Section
    public int TrashPickupId { get; set; }
    public string? TrashRemoval { get; set; }

    // Additional Amenities Section
    public string? Amenities { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

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
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }


    public PropertyResponseDto(Property property)
    {
        PropertyId = property.PropertyId;
        OrganizationId = property.OrganizationId;
        PropertyCode = property.PropertyCode;
        PropertyLeaseTypeId = (int)property.PropertyLeaseType;
        Owner1Id = property.Owner1Id;
        Owner2Id = property.Owner2Id;
        Owner3Id = property.Owner3Id;
        VendorId = property.VendorId;
        AvailableFrom = property.AvailableFrom;
        AvailableUntil = property.AvailableUntil;
        MinStay = property.MinStay;
        MaxStay = property.MaxStay;
        CheckInTimeId = (int)property.CheckInTime;
        CheckOutTimeId = (int)property.CheckOutTime;
        PropertyStyleId = (int)property.PropertyStyle;
        PropertyTypeId = (int)property.PropertyType;
        PropertyStatusId = (int)property.PropertyStatus;
        OfficeId = property.OfficeId;
        OfficeName = property.OfficeName;
        BuildingId = property.BuildingId;
        RegionId = property.RegionId;
        AreaId = property.AreaId;
        Latitude = property.Latitude;
        Longitude = property.Longitude;
        MonthlyRate = property.MonthlyRate;
        DailyRate = property.DailyRate;
        DepartureFee = property.DepartureFee;
        MaidServiceFee = property.MaidServiceFee;
        PetFee = property.PetFee;
        ExtraFee = property.ExtraFee;
        ExtraFeeName = property.ExtraFeeName;
        BldgNo = property.BldgNo;
        UnitLevel = property.UnitLevel;
        Bedrooms = property.Bedrooms;
        Bathrooms = property.Bathrooms;
        Accomodates = property.Accomodates;
        SquareFeet = property.SquareFeet;
        BedroomId1 = property.BedroomId1;
        BedroomId2 = property.BedroomId2;
        BedroomId3 = property.BedroomId3;
        BedroomId4 = property.BedroomId4;
        Sofabed = property.Sofabed;
        Address1 = property.Address1;
        Address2 = property.Address2;
        Suite = property.Suite;
        City = property.City;
        State = property.State;
        Zip = property.Zip;
        Phone = property.Phone;
        CommunityAddress = property.CommunityAddress;
        Neighborhood = property.Neighborhood;
        CrossStreet = property.CrossStreet;
        View = property.View;
        Mailbox = property.Mailbox;
        Unfurnished = property.Unfurnished;
        Heating = property.Heating;
        Ac = property.Ac;
        Elevator = property.Elevator;
        Security = property.Security;
        Gated = property.Gated;
        PetsAllowed = property.PetsAllowed;
        DogsOkay = property.DogsOkay;
        CatsOkay = property.CatsOkay;
        PoundLimit = property.PoundLimit;
        Smoking = property.Smoking;
        Parking = property.Parking;
        ParkingNotes = property.ParkingNotes;
        AlarmCode = property.AlarmCode;
        UnitMstrCode = property.UnitMstrCode;
        BldgMstrCode = property.BldgMstrCode;
        BldgTenantCode = property.BldgTenantCode;
        MailRoomCode = property.MailRoomCode;
        GarageCode = property.GarageCode;
        GateCode = property.GateCode;
        TrashCode = property.TrashCode;
        StorageCode = property.StorageCode;
        Kitchen = property.Kitchen;
        Oven = property.Oven;
        Refrigerator = property.Refrigerator;
        Microwave = property.Microwave;
        Dishwasher = property.Dishwasher;
        Bathtub = property.Bathtub;
        WasherDryerInUnit = property.WasherDryerInUnit;
        WasherDryerInBldg = property.WasherDryerInBldg;
        Tv = property.Tv;
        Cable = property.Cable;
        Dvd = property.Dvd;
        Streaming = property.Streaming;
        FastInternet = property.FastInternet;
        InternetNetwork = property.InternetNetwork;
        InternetPassword = property.InternetPassword;
        Deck = property.Deck;
        Patio = property.Patio;
        Yard = property.Yard;
        Garden = property.Garden;
        CommonPool = property.CommonPool;
        PrivatePool = property.PrivatePool;
        Jacuzzi = property.Jacuzzi;
        Sauna = property.Sauna;
        Gym = property.Gym;
        TrashPickupId = property.TrashPickupId;
        TrashRemoval = property.TrashRemoval;
        Amenities = property.Amenities;
        Description = property.Description;
        Notes = property.Notes;
        OnlineCleanerUserId = property.OnlineCleanerUserId;
        OnlineCleaningDate = property.OnlineCleaningDate;
        OnlineCarpetUserId = property.OnlineCarpetUserId;
        OnlineCarpetDate = property.OnlineCarpetDate;
        OnlineInspectorUserId = property.OnlineInspectorUserId;
        OnlineInspectingDate = property.OnlineInspectingDate;
        OfflineCleanerUserId = property.OfflineCleanerUserId;
        OfflineCleaningDate = property.OfflineCleaningDate;
        OfflineCarpetUserId = property.OfflineCarpetUserId;
        OfflineCarpetDate = property.OfflineCarpetDate;
        OfflineInspectorUserId = property.OfflineInspectorUserId;
        OfflineInspectingDate = property.OfflineInspectingDate;
        IsActive = property.IsActive;
        CreatedOn = property.CreatedOn;
        CreatedBy = property.CreatedBy;
        ModifiedOn = property.ModifiedOn;
        ModifiedBy = property.ModifiedBy;
    }
}
