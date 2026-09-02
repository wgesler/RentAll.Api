namespace RentAll.Api.Dtos.Properties.Properties;

public class CreateExternalPropertyDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;

    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;

    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }

    public int Accommodates { get; set; }

    public int SquareFeet { get; set; }
    public int PropertyStyleId { get; set; }
    public int PropertyTypeId { get; set; }

    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal? DepartureFee { get; set; }
    public decimal? MaidServiceFee { get; set; }
    public decimal? PetFee { get; set; }

    public string ExternalCalendar { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool? IsActive { get; set; }
    public int? MinStay { get; set; }
    public int? MaxStay { get; set; }
    public int? CheckInTimeId { get; set; }
    public int? CheckOutTimeId { get; set; }
    public int? BedroomId1 { get; set; }
    public int? BedroomId2 { get; set; }
    public int? BedroomId3 { get; set; }
    public int? BedroomId4 { get; set; }

    public string? Neighborhood { get; set; }
    public string? CrossStreet { get; set; }
    public string? View { get; set; }
    public string? Mailbox { get; set; }

    public bool? Unfurnished { get; set; }
    public bool? Heating { get; set; }
    public bool? Ac { get; set; }
    public bool? Elevator { get; set; }
    public bool? Security { get; set; }
    public bool? Gated { get; set; }
    public bool? PetsAllowed { get; set; }
    public bool? DogsOkay { get; set; }
    public bool? CatsOkay { get; set; }
    public string? PoundLimit { get; set; }
    public bool? Smoking { get; set; }
    public bool? Parking { get; set; }
    public string? ParkingNotes { get; set; }

    public bool? Kitchen { get; set; }
    public bool? Oven { get; set; }
    public bool? Refrigerator { get; set; }
    public bool? Microwave { get; set; }
    public bool? Dishwasher { get; set; }
    public bool? Bathtub { get; set; }
    public bool? WasherDryerInUnit { get; set; }
    public bool? WasherDryerInBldg { get; set; }
    public bool? Tv { get; set; }
    public bool? Cable { get; set; }
    public bool? Dvd { get; set; }
    public bool? Streaming { get; set; }
    public bool? FastInternet { get; set; }
    public bool? Deck { get; set; }
    public bool? Patio { get; set; }
    public bool? Yard { get; set; }
    public bool? Garden { get; set; }
    public bool? CommonPool { get; set; }
    public bool? PrivatePool { get; set; }
    public bool? Jacuzzi { get; set; }
    public bool? Sauna { get; set; }
    public bool? Gym { get; set; }
    public string? Amenities { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (VendorId == Guid.Empty)
            return (false, "VendorId is required");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "PropertyCode is required");

        if (string.IsNullOrWhiteSpace(Address1))
            return (false, "Address1 is required");

        if (string.IsNullOrWhiteSpace(City))
            return (false, "City is required");

        if (string.IsNullOrWhiteSpace(State))
            return (false, "State is required");

        if (string.IsNullOrWhiteSpace(Zip))
            return (false, "Zip is required");

        if (Bedrooms < 0)
            return (false, "Bedrooms must be >= 0");

        if (Bathrooms < 0)
            return (false, "Bathrooms must be >= 0");

        if (Accommodates < 0)
            return (false, "Accommodates must be >= 0");

        if (SquareFeet < 0)
            return (false, "SquareFeet must be >= 0");

        if (!Enum.IsDefined(typeof(PropertyStyle), PropertyStyleId))
            return (false, $"Invalid PropertyStyleId value: {PropertyStyleId}");

        if (!Enum.IsDefined(typeof(PropertyType), PropertyTypeId))
            return (false, $"Invalid PropertyTypeId value: {PropertyTypeId}");

        if (PropertyTypeId == (int)PropertyType.Unspecified)
            return (false, "PropertyTypeId is required");

        if (MonthlyRate < 0)
            return (false, "MonthlyRate must be >= 0");

        if (DailyRate < 0)
            return (false, "DailyRate must be >= 0");

        if (DepartureFee is < 0)
            return (false, "DepartureFee must be >= 0");

        if (MaidServiceFee is < 0)
            return (false, "MaidServiceFee must be >= 0");

        if (PetFee is < 0)
            return (false, "PetFee must be >= 0");

        if (ExternalCalendar == null)
            return (false, "ExternalCalendar is required");

        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");

        if (MinStay is < 0)
            return (false, "MinStay must be >= 0");

        if (MaxStay is < 0)
            return (false, "MaxStay must be >= 0");

        if (CheckInTimeId.HasValue && !Enum.IsDefined(typeof(CheckInTime), CheckInTimeId.Value))
            return (false, $"Invalid CheckInTimeId value: {CheckInTimeId.Value}");

        if (CheckOutTimeId.HasValue && !Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId.Value))
            return (false, $"Invalid CheckOutTimeId value: {CheckOutTimeId.Value}");

        var bedroomValidation = ValidateBedroomId(BedroomId1, 1)
            ?? ValidateBedroomId(BedroomId2, 2)
            ?? ValidateBedroomId(BedroomId3, 3)
            ?? ValidateBedroomId(BedroomId4, 4);
        if (bedroomValidation != null)
            return (false, bedroomValidation);

        return (true, null);
    }

    public CreatePropertyDto ToCreatePropertyDto(string propertyCode)
    {
        return new CreatePropertyDto
        {
            OrganizationId = OrganizationId,
            PropertyCode = propertyCode,
            PropertyLeaseTypeId = (int)PropertyLeaseType.Direct,
            VendorId = VendorId,
            IsActive = IsActive ?? true,
            MinStay = MinStay ?? 0,
            MaxStay = MaxStay ?? 0,
            CheckInTimeId = CheckInTimeId ?? (int)CheckInTime.FourPM,
            CheckOutTimeId = CheckOutTimeId ?? (int)CheckOutTime.ElevenAM,
            PropertyStyleId = PropertyStyleId,
            PropertyTypeId = PropertyTypeId,
            PropertyStatusId = (int)PropertyStatus.Vacant,
            NoticeToVacateId = 0,
            NoticeStatusId = (int)NoticeStatusType.None,
            OfficeId = OfficeId,
            Latitude = 0m,
            Longitude = 0m,
            ExternalCalendar = TrimOrNull(ExternalCalendar),
            MonthlyRate = MonthlyRate,
            DailyRate = DailyRate,
            DepartureFee = DepartureFee ?? 0m,
            MaidServiceFee = MaidServiceFee ?? 0m,
            PetFee = PetFee ?? 0m,
            UnitLevel = 1,
            Bedrooms = Bedrooms,
            Bathrooms = Bathrooms,
            Accommodates = Accommodates,
            SquareFeet = SquareFeet,
            BedroomId1 = BedroomId1 ?? 0,
            BedroomId2 = BedroomId2 ?? 0,
            BedroomId3 = BedroomId3 ?? 0,
            BedroomId4 = BedroomId4 ?? 0,
            Address1 = Address1.Trim(),
            Address2 = TrimOrNull(Address2),
            Suite = TrimOrNull(Suite),
            City = City.Trim(),
            State = State.Trim(),
            Zip = Zip.Trim(),
            Neighborhood = TrimOrNull(Neighborhood),
            CrossStreet = TrimOrNull(CrossStreet),
            View = TrimOrNull(View),
            Mailbox = TrimOrNull(Mailbox),
            Unfurnished = Unfurnished ?? false,
            Heating = Heating ?? false,
            Ac = Ac ?? false,
            Elevator = Elevator ?? false,
            Security = Security ?? false,
            Gated = Gated ?? false,
            PetsAllowed = PetsAllowed ?? false,
            DogsOkay = DogsOkay ?? false,
            CatsOkay = CatsOkay ?? false,
            PoundLimit = (PoundLimit ?? string.Empty).Trim(),
            Smoking = Smoking ?? false,
            Parking = Parking ?? false,
            ParkingNotes = TrimOrNull(ParkingNotes),
            Kitchen = Kitchen ?? false,
            Oven = Oven ?? false,
            Refrigerator = Refrigerator ?? false,
            Microwave = Microwave ?? false,
            Dishwasher = Dishwasher ?? false,
            Bathtub = Bathtub ?? false,
            WasherDryerInUnit = WasherDryerInUnit ?? false,
            WasherDryerInBldg = WasherDryerInBldg ?? false,
            Tv = Tv ?? false,
            Cable = Cable ?? false,
            Dvd = Dvd ?? false,
            Streaming = Streaming ?? false,
            FastInternet = FastInternet ?? false,
            Deck = Deck ?? false,
            Patio = Patio ?? false,
            Yard = Yard ?? false,
            Garden = Garden ?? false,
            CommonPool = CommonPool ?? false,
            PrivatePool = PrivatePool ?? false,
            Jacuzzi = Jacuzzi ?? false,
            Sauna = Sauna ?? false,
            Gym = Gym ?? false,
            Amenities = TrimOrNull(Amenities),
            Description = Description.Trim()
        };
    }

    private static string? ValidateBedroomId(int? bedroomId, int bedroomNumber)
    {
        if (!bedroomId.HasValue)
            return null;

        if (!Enum.IsDefined(typeof(BedSizeType), bedroomId.Value))
            return $"Invalid BedroomId{bedroomNumber} value: {bedroomId.Value}";

        return null;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
