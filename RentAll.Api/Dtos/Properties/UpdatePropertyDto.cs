using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties;

public class UpdatePropertyDto
{
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid Owner1Id { get; set; }
    public Guid? Owner2Id { get; set; }
    public Guid? Owner3Id { get; set; }

    // Availability Section 
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
    public int MinStay { get; set; }
    public int MaxStay { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }


    // Property Classification
    public int PropertyStyleId { get; set; }
    public int PropertyTypeId { get; set; }
    public int PropertyStatusId { get; set; }
    public int OfficeId { get; set; }
    public int? BuildingId { get; set; }
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }

    // Rates & Fees
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal ExtraFee { get; set; }
    public string ExtraFeeName { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accomodates { get; set; }
    public int SquareFeet { get; set; }
    public int BedroomId1 { get; set; }
    public int BedroomId2 { get; set; }
    public int BedroomId3 { get; set; }
    public int BedroomId4 { get; set; }

    // Address Section
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string? Phone { get; set; }
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
    public bool Alarm { get; set; }
    public string? AlarmCode { get; set; }
    public bool KeypadAccess { get; set; }
    public string? MasterKeyCode { get; set; }
    public string? TenantKeyCode { get; set; }

    // Kitchen & Bath
    public bool Kitchen { get; set; }
    public bool Oven { get; set; }
    public bool Refrigerator { get; set; }
    public bool Microwave { get; set; }
    public bool Dishwasher { get; set; }
    public bool Bathtub { get; set; }
    public bool WasherDryer { get; set; }
    public bool Sofabeds { get; set; }

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
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "Property ID is required");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "Property Code is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (Owner1Id == Guid.Empty)
            return (false, "Owner1 ID is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        // Validate enum values
        if (!Enum.IsDefined(typeof(CheckInTime), CheckInTimeId))
            return (false, $"Invalid CheckIn Time value: {CheckInTimeId}");

        if (!Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId))
            return (false, $"Invalid CheckOutTime value: {CheckOutTimeId}");

        if (!Enum.IsDefined(typeof(PropertyStyle), PropertyStyleId))
            return (false, $"Invalid PropertyStyle value: {PropertyStyleId}");

        if (!Enum.IsDefined(typeof(PropertyType), PropertyTypeId))
            return (false, $"Invalid PropertyType value: {PropertyTypeId}");

        if (!Enum.IsDefined(typeof(PropertyStatus), PropertyStatusId))
            return (false, $"Invalid PropertyStatus value: {PropertyStatusId}");

        return (true, null);
    }

    public Property ToModel(Guid currentUser)
    {
        return new Property
        {
            OrganizationId = OrganizationId,
            PropertyId = PropertyId,
            PropertyCode = PropertyCode,
            Owner1Id = Owner1Id,
            Owner2Id = Owner2Id,
            Owner3Id = Owner3Id,
            AvailableFrom = AvailableFrom,
            AvailableUntil = AvailableUntil,
            MinStay = MinStay,
            MaxStay = MaxStay,
            CheckInTime = (CheckInTime)CheckInTimeId,
            CheckOutTime = (CheckOutTime)CheckOutTimeId,
            PropertyStyle = (PropertyStyle)PropertyStyleId,
            PropertyType = (PropertyType)PropertyTypeId,
            PropertyStatus = (PropertyStatus)PropertyStatusId,
            OfficeId = OfficeId,
            BuildingId = BuildingId,
            RegionId = RegionId,
            AreaId = AreaId,
            MonthlyRate = MonthlyRate,
            DailyRate = DailyRate,
            DepartureFee = DepartureFee,
            MaidServiceFee = MaidServiceFee,
            PetFee = PetFee,
            ExtraFee = ExtraFee,
            ExtraFeeName = ExtraFeeName ?? string.Empty,
            Bedrooms = Bedrooms,
            Bathrooms = Bathrooms,
            Accomodates = Accomodates,
            SquareFeet = SquareFeet,
            Bedroom1 = (BedSizeType)BedroomId1,
            Bedroom2 = (BedSizeType)BedroomId2,
            Bedroom3 = (BedSizeType)BedroomId3,
            Bedroom4 = (BedSizeType)BedroomId4,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            Neighborhood = Neighborhood,
            CrossStreet = CrossStreet,
            View = View,
            Mailbox = Mailbox,
            Unfurnished = Unfurnished,
            Heating = Heating,
            Ac = Ac,
            Elevator = Elevator,
            Security = Security,
            Gated = Gated,
            PetsAllowed = PetsAllowed,
            DogsOkay = DogsOkay,
            CatsOkay = CatsOkay,
            PoundLimit = PoundLimit ?? string.Empty,
            Smoking = Smoking,
            Parking = Parking,
            ParkingNotes = ParkingNotes,
            Alarm = Alarm,
            AlarmCode = AlarmCode,
            KeypadAccess = KeypadAccess,
            MasterKeyCode = MasterKeyCode,
            TenantKeyCode = TenantKeyCode,
            Kitchen = Kitchen,
            Oven = Oven,
            Refrigerator = Refrigerator,
            Microwave = Microwave,
            Dishwasher = Dishwasher,
            Bathtub = Bathtub,
            WasherDryer = WasherDryer,
            Sofabeds = Sofabeds,
            Tv = Tv,
            Cable = Cable,
            Dvd = Dvd,
            Streaming = Streaming,
            FastInternet = FastInternet,
            InternetNetwork = InternetNetwork,
            InternetPassword = InternetPassword,
            Deck = Deck,
            Patio = Patio,
            Yard = Yard,
            Garden = Garden,
            CommonPool = CommonPool,
            PrivatePool = PrivatePool,
            Jacuzzi = Jacuzzi,
            Sauna = Sauna,
            Gym = Gym,
            TrashPickupId = TrashPickupId,
            TrashRemoval = TrashRemoval,
            Amenities = Amenities,
            Description = Description,
            Notes = Notes,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
