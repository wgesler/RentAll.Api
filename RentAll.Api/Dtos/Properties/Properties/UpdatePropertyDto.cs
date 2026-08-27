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
    public string? ConfirmationNo { get; set; }
    public int MinStay { get; set; }
    public int MaxStay { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }

    // Property Classification
    public int PropertyStyleId { get; set; }
    public int PropertyTypeId { get; set; }
    public int PropertyStatusId { get; set; }
    public int NoticeToVacateId { get; set; }
    public int NoticeStatusId { get; set; }
    public int OfficeId { get; set; }
    public int? BuildingId { get; set; }
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ExternalCalendar { get; set; }

    // Rates & Fees
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal MaidServiceFee { get; set; }
    public decimal PetFee { get; set; }
    public string? BldgNo { get; set; }
    public int UnitLevel { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int Accommodates { get; set; }
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

    public static UpdatePropertyDto FromProperty(Property property)
    {
        return new UpdatePropertyDto
        {
            OrganizationId = property.OrganizationId,
            PropertyId = property.PropertyId,
            PropertyCode = property.PropertyCode,
            PropertyLeaseTypeId = NormalizeEnumId(property.PropertyLeaseType, 0),
            Owner1Id = property.Owner1Id,
            Owner2Id = property.Owner2Id,
            Owner3Id = property.Owner3Id,
            VendorId = property.VendorId,
            AvailableFrom = property.AvailableFrom,
            AvailableUntil = property.AvailableUntil,
            ConfirmationNo = property.ConfirmationNo,
            MinStay = property.MinStay,
            MaxStay = property.MaxStay,
            CheckInTimeId = NormalizeEnumId(property.CheckInTime, 0),
            CheckOutTimeId = NormalizeEnumId(property.CheckOutTime, 0),
            PropertyStyleId = NormalizeEnumId(property.PropertyStyle, 0),
            PropertyTypeId = NormalizeEnumId(property.PropertyType, 0),
            PropertyStatusId = NormalizeEnumId(property.PropertyStatus, 0),
            NoticeToVacateId = NormalizeEnumId(property.NoticeToVacate, 0),
            NoticeStatusId = NormalizeEnumId(property.NoticeStatus, (int)NoticeStatusType.None),
            OfficeId = property.OfficeId,
            BuildingId = property.BuildingId,
            RegionId = property.RegionId,
            AreaId = property.AreaId,
            Latitude = property.Latitude,
            Longitude = property.Longitude,
            ExternalCalendar = property.ExternalCalendar,
            MonthlyRate = property.MonthlyRate,
            DailyRate = property.DailyRate,
            DepartureFee = property.DepartureFee,
            MaidServiceFee = property.MaidServiceFee,
            PetFee = property.PetFee,
            BldgNo = property.BldgNo,
            UnitLevel = property.UnitLevel,
            Bedrooms = property.Bedrooms,
            Bathrooms = property.Bathrooms,
            Accommodates = property.Accommodates,
            SquareFeet = property.SquareFeet,
            BedroomId1 = property.BedroomId1,
            BedroomId2 = property.BedroomId2,
            BedroomId3 = property.BedroomId3,
            BedroomId4 = property.BedroomId4,
            Sofabed = property.Sofabed,
            Address1 = property.Address1,
            Address2 = property.Address2,
            Suite = property.Suite,
            City = property.City,
            State = property.State,
            Zip = property.Zip,
            Phone = property.Phone,
            CommunityAddress = property.CommunityAddress,
            Neighborhood = property.Neighborhood,
            CrossStreet = property.CrossStreet,
            View = property.View,
            Mailbox = property.Mailbox,
            Unfurnished = property.Unfurnished,
            Heating = property.Heating,
            Ac = property.Ac,
            Elevator = property.Elevator,
            Security = property.Security,
            Gated = property.Gated,
            PetsAllowed = property.PetsAllowed,
            DogsOkay = property.DogsOkay,
            CatsOkay = property.CatsOkay,
            PoundLimit = property.PoundLimit,
            Smoking = property.Smoking,
            Parking = property.Parking,
            ParkingNotes = property.ParkingNotes,
            AlarmCode = property.AlarmCode,
            UnitMstrCode = property.UnitMstrCode,
            BldgMstrCode = property.BldgMstrCode,
            BldgTenantCode = property.BldgTenantCode,
            MailRoomCode = property.MailRoomCode,
            GateCode = property.GateCode,
            TrashCode = property.TrashCode,
            StorageCode = property.StorageCode,
            Kitchen = property.Kitchen,
            Oven = property.Oven,
            Refrigerator = property.Refrigerator,
            Microwave = property.Microwave,
            Dishwasher = property.Dishwasher,
            Bathtub = property.Bathtub,
            WasherDryerInUnit = property.WasherDryerInUnit,
            WasherDryerInBldg = property.WasherDryerInBldg,
            Tv = property.Tv,
            Cable = property.Cable,
            Dvd = property.Dvd,
            Streaming = property.Streaming,
            FastInternet = property.FastInternet,
            InternetNetwork = property.InternetNetwork,
            InternetPassword = property.InternetPassword,
            Deck = property.Deck,
            Patio = property.Patio,
            Yard = property.Yard,
            Garden = property.Garden,
            CommonPool = property.CommonPool,
            PrivatePool = property.PrivatePool,
            Jacuzzi = property.Jacuzzi,
            Sauna = property.Sauna,
            Gym = property.Gym,
            TrashPickupId = property.TrashPickupId,
            TrashRemoval = property.TrashRemoval,
            Amenities = property.Amenities,
            Description = property.Description,
            Notes = property.Notes,
            onCleanerUserId = property.onCleanerUserId,
            onCleaningDate = property.onCleaningDate,
            onCarpetUserId = property.onCarpetUserId,
            onCarpetDate = property.onCarpetDate,
            onInspectorUserId = property.onInspectorUserId,
            onInspectingDate = property.onInspectingDate,
            offCleanerUserId = property.offCleanerUserId,
            offCleaningDate = property.offCleaningDate,
            offCarpetUserId = property.offCarpetUserId,
            offCarpetDate = property.offCarpetDate,
            offInspectorUserId = property.offInspectorUserId,
            offInspectingDate = property.offInspectingDate,
            OnlineChecked = property.OnlineChecked,
            OfflineChecked = property.OfflineChecked,
            IsActive = property.IsActive
        };
    }

    private static int NormalizeEnumId<TEnum>(TEnum value, int fallback) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value) ? Convert.ToInt32(value) : fallback;
    }

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

        if (!Enum.IsDefined(typeof(ReservationNotice), NoticeToVacateId))
            return (false, $"Invalid NoticeToVacateId value: {NoticeToVacateId}");

        if (!Enum.IsDefined(typeof(NoticeStatusType), NoticeStatusId))
            return (false, $"Invalid NoticeStatusId value: {NoticeStatusId}");

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
            ConfirmationNo = ConfirmationNo,
            MinStay = MinStay,
            MaxStay = MaxStay,
            CheckInTime = (CheckInTime)CheckInTimeId,
            CheckOutTime = (CheckOutTime)CheckOutTimeId,
            PropertyStyle = (PropertyStyle)PropertyStyleId,
            PropertyType = (PropertyType)PropertyTypeId,
            PropertyStatus = (PropertyStatus)PropertyStatusId,
            NoticeToVacate = (ReservationNotice)NoticeToVacateId,
            NoticeStatus = (NoticeStatusType)NoticeStatusId,
            OfficeId = OfficeId,
            BuildingId = BuildingId,
            RegionId = RegionId,
            AreaId = AreaId,
            Latitude = Latitude,
            Longitude = Longitude,
            ExternalCalendar = ExternalCalendar,
            MonthlyRate = MonthlyRate,
            DailyRate = DailyRate,
            DepartureFee = DepartureFee,
            MaidServiceFee = MaidServiceFee,
            PetFee = PetFee,
            BldgNo = BldgNo,
            UnitLevel = UnitLevel,
            Bedrooms = Bedrooms,
            Bathrooms = Bathrooms,
            Accommodates = Accommodates,
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
            onCleanerUserId = onCleanerUserId,
            onCleaningDate = onCleaningDate,
            onCarpetUserId = onCarpetUserId,
            onCarpetDate = onCarpetDate,
            onInspectorUserId = onInspectorUserId,
            onInspectingDate = onInspectingDate,
            offCleanerUserId = offCleanerUserId,
            offCleaningDate = offCleaningDate,
            offCarpetUserId = offCarpetUserId,
            offCarpetDate = offCarpetDate,
            offInspectorUserId = offInspectorUserId,
            offInspectingDate = offInspectingDate,
            OnlineChecked = OnlineChecked,
            OfflineChecked = OfflineChecked,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}
