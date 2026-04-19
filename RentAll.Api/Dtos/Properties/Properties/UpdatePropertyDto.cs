namespace RentAll.Api.Dtos.Properties.Properties;

public class UpdatePropertyDto
{
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
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

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PropertyId == Guid.Empty)
            return (false, "Property ID is required");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "Property Code is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

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

        if (!Enum.IsDefined(typeof(PropertyLeaseType), PropertyLeaseTypeId))
            return (false, $"Invalid PropertyLeaseType value: {PropertyLeaseTypeId}");

        return (true, null);
    }

    public Property ToModel(Guid currentUser)
    {
        return new Property
        {
            OrganizationId = OrganizationId,
            PropertyId = PropertyId,
            PropertyCode = PropertyCode,
            PropertyLeaseType = (PropertyLeaseType)PropertyLeaseTypeId,
            Owner1Id = Owner1Id,
            Owner2Id = Owner2Id,
            Owner3Id = Owner3Id,
            VendorId = VendorId,
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
            Latitude = Latitude,
            Longitude = Longitude,
            MonthlyRate = MonthlyRate,
            DailyRate = DailyRate,
            DepartureFee = DepartureFee,
            MaidServiceFee = MaidServiceFee,
            PetFee = PetFee,
            ExtraFee = ExtraFee,
            ExtraFeeName = ExtraFeeName ?? string.Empty,
            BldgNo = BldgNo,
            UnitLevel = UnitLevel,
            Bedrooms = Bedrooms,
            Bathrooms = Bathrooms,
            Accomodates = Accomodates,
            SquareFeet = SquareFeet,
            BedroomId1 = BedroomId1,
            BedroomId2 = BedroomId2,
            BedroomId3 = BedroomId3,
            BedroomId4 = BedroomId4,
            Sofabed = Sofabed,
            Address1 = Address1,
            Address2 = Address2,
            Suite = Suite,
            City = City,
            State = State,
            Zip = Zip,
            Phone = Phone,
            CommunityAddress = CommunityAddress,
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
            AlarmCode = AlarmCode,
            UnitMstrCode = UnitMstrCode,
            BldgMstrCode = BldgMstrCode,
            BldgTenantCode = BldgTenantCode,
            MailRoomCode = MailRoomCode,
            GarageCode = GarageCode,
            GateCode = GateCode,
            TrashCode = TrashCode,
            StorageCode = StorageCode,
            Kitchen = Kitchen,
            Oven = Oven,
            Refrigerator = Refrigerator,
            Microwave = Microwave,
            Dishwasher = Dishwasher,
            Bathtub = Bathtub,
            WasherDryerInUnit = WasherDryerInUnit,
            WasherDryerInBldg = WasherDryerInBldg,
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
            OnlineCleanerUserId = OnlineCleanerUserId,
            OnlineCleaningDate = OnlineCleaningDate,
            OnlineCarpetUserId = OnlineCarpetUserId,
            OnlineCarpetDate = OnlineCarpetDate,
            OnlineInspectorUserId = OnlineInspectorUserId,
            OnlineInspectingDate = OnlineInspectingDate,
            OfflineCleanerUserId = OfflineCleanerUserId,
            OfflineCleaningDate = OfflineCleaningDate,
            OfflineCarpetUserId = OfflineCarpetUserId,
            OfflineCarpetDate = OfflineCarpetDate,
            OfflineInspectorUserId = OfflineInspectorUserId,
            OfflineInspectingDate = OfflineInspectingDate,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
