using RentAll.Api.Dtos.Properties.PropertyPhotos;
using RentAll.Domain;

namespace RentAll.Api.Dtos.Properties.Properties;

public class CreateExternalPropertyDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public ExternalPropertyContactDto? Owner1 { get; set; }
    public ExternalPropertyContactDto? Owner2 { get; set; }
    public ExternalPropertyContactDto? Owner3 { get; set; }
    public ExternalPropertyContactDto? Vendor { get; set; }
    public List<ExternalPropertyPhotoUrlItemDto>? Photos { get; set; }

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

    public string? ExternalCalendar { get; set; }
    public List<string> ExternalCalendars { get; set; } = [];
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
        var errors = CollectErrors();
        return errors.Count == 0 ? (true, null) : (false, ExternalPropertyIntakeErrors.Join(errors));
    }

    public List<string> CollectErrors()
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(typeof(PropertyLeaseType), PropertyLeaseTypeId))
            errors.Add($"propertyLeaseTypeId must be 0=PropertyManagement, 1=Direct, or 2=ThirdParty. Received {PropertyLeaseTypeId}.");

        var leaseType = Enum.IsDefined(typeof(PropertyLeaseType), PropertyLeaseTypeId) ? (PropertyLeaseType)PropertyLeaseTypeId : (PropertyLeaseType)(-1);
        if (leaseType == PropertyLeaseType.PropertyManagement)
        {
            if (Owner1 == null)
                errors.Add("owner1 is required when propertyLeaseTypeId is 0 (PropertyManagement).");
            else
                errors.AddRange(Owner1.CollectErrors("owner1"));

            if (Vendor != null)
                errors.Add("vendor is not allowed when propertyLeaseTypeId is 0 (PropertyManagement).");
        }
        else if (leaseType is PropertyLeaseType.Direct or PropertyLeaseType.ThirdParty)
        {
            if (Vendor == null)
                errors.Add($"vendor is required when propertyLeaseTypeId is {(int)leaseType} ({leaseType}).");
            else
                errors.AddRange(Vendor.CollectErrors("vendor"));
        }

        if (Owner2 != null)
            errors.AddRange(Owner2.CollectErrors("owner2"));

        if (Owner3 != null)
            errors.AddRange(Owner3.CollectErrors("owner3"));

        if (Owner1 != null && leaseType is PropertyLeaseType.Direct or PropertyLeaseType.ThirdParty)
            errors.AddRange(Owner1.CollectErrors("owner1"));

        var (photosAreValid, photosError) = ExternalPropertyPhotosValidator.ValidatePhotos(Photos);
        if (!photosAreValid && photosError != null)
            errors.Add(photosError);

        if (string.IsNullOrWhiteSpace(PropertyCode))
            errors.Add("propertyCode is required.");
        if (string.IsNullOrWhiteSpace(Address1))
            errors.Add("address1 is required.");
        if (string.IsNullOrWhiteSpace(City))
            errors.Add("city is required.");
        if (string.IsNullOrWhiteSpace(State))
            errors.Add("state is required.");
        else if (!UsStateCode.IsRecognized(State))
            errors.Add($"state must be a 2-letter US code or full state name. Received \"{State}\".");
        if (string.IsNullOrWhiteSpace(Zip))
            errors.Add("zip is required.");
        else if (Zip.Trim().Length > 10)
            errors.Add($"zip is {Zip.Trim().Length} characters; max is 10. Received \"{Zip.Trim()}\".");

        if (Bedrooms < 0)
            errors.Add($"bedrooms must be >= 0. Received {Bedrooms}.");
        if (Bathrooms < 0)
            errors.Add($"bathrooms must be >= 0. Received {Bathrooms}.");
        if (Accommodates < 0)
            errors.Add($"accommodates must be >= 0. Received {Accommodates}.");
        if (SquareFeet < 0)
            errors.Add($"squareFeet must be >= 0. Received {SquareFeet}.");

        if (!Enum.IsDefined(typeof(PropertyStyle), PropertyStyleId))
            errors.Add($"propertyStyleId must be 0=Standard, 1=Corporate, or 2=Vacation. Received {PropertyStyleId}.");
        if (!Enum.IsDefined(typeof(PropertyType), PropertyTypeId) || PropertyTypeId == (int)PropertyType.Unspecified)
            errors.Add($"propertyTypeId must be 1-17 (1=Apartment, 8=House, 5=Condo). 0=Unspecified is not allowed. Received {PropertyTypeId}.");

        if (MonthlyRate < 0)
            errors.Add($"monthlyRate must be >= 0. Received {MonthlyRate}.");
        if (DailyRate < 0)
            errors.Add($"dailyRate must be >= 0. Received {DailyRate}.");
        if (DepartureFee is < 0)
            errors.Add($"departureFee must be >= 0. Received {DepartureFee}.");
        if (MaidServiceFee is < 0)
            errors.Add($"maidServiceFee must be >= 0. Received {MaidServiceFee}.");
        if (PetFee is < 0)
            errors.Add($"petFee must be >= 0. Received {PetFee}.");

        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("description is required.");
        if (MinStay is < 0)
            errors.Add($"minStay must be >= 0. Received {MinStay}.");
        if (MaxStay is < 0)
            errors.Add($"maxStay must be >= 0. Received {MaxStay}.");
        if (CheckInTimeId.HasValue && !Enum.IsDefined(typeof(CheckInTime), CheckInTimeId.Value))
            errors.Add($"checkInTimeId must be 0=11AM through 6=5PM. Received {CheckInTimeId.Value}.");
        if (CheckOutTimeId.HasValue && !Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId.Value))
            errors.Add($"checkOutTimeId must be 1=8AM through 8=3PM. Received {CheckOutTimeId.Value}.");

        AddBedroomIdError(errors, BedroomId1, "bedroomId1");
        AddBedroomIdError(errors, BedroomId2, "bedroomId2");
        AddBedroomIdError(errors, BedroomId3, "bedroomId3");
        AddBedroomIdError(errors, BedroomId4, "bedroomId4");

        return errors;
    }

    public static (bool IsValid, string? ErrorMessage) ValidateLeaseTypeContacts(int propertyLeaseTypeId, Guid? owner1Id, Guid? propertyVendorContactId, bool vendorContactInRequest)
    {
        if (!Enum.IsDefined(typeof(PropertyLeaseType), propertyLeaseTypeId))
            return (false, $"Invalid PropertyLeaseTypeId value: {propertyLeaseTypeId}");

        var leaseType = (PropertyLeaseType)propertyLeaseTypeId;
        if (leaseType == PropertyLeaseType.PropertyManagement)
        {
            if (owner1Id == null || owner1Id == Guid.Empty)
                return (false, "Owner1 is required for PropertyManagement lease type");

            if (vendorContactInRequest || (propertyVendorContactId != null && propertyVendorContactId != Guid.Empty))
                return (false, "Vendor is not allowed for PropertyManagement lease type");
        }
        else if (leaseType is PropertyLeaseType.Direct or PropertyLeaseType.ThirdParty)
        {
            if (propertyVendorContactId == null || propertyVendorContactId == Guid.Empty)
                return (false, "Vendor is required for Direct and ThirdParty lease types");
        }

        return (true, null);
    }

    public CreatePropertyDto ToCreatePropertyDto(string propertyCode, ExternalPropertyIntakeContext context, Guid? owner1Id, Guid? owner2Id, Guid? owner3Id, Guid? propertyVendorContactId)
    {
        return new CreatePropertyDto
        {
            OrganizationId = context.OrganizationId,
            PropertyCode = propertyCode,
            PropertyLeaseTypeId = PropertyLeaseTypeId,
            Owner1Id = owner1Id,
            Owner2Id = owner2Id,
            Owner3Id = owner3Id,
            VendorId = propertyVendorContactId,
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
            OfficeId = context.OfficeId,
            Latitude = 0m,
            Longitude = 0m,
            ExternalCalendars = PropertyICalDto.MergeCalendarInputs(ExternalCalendars, ExternalCalendar),
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
            State = UsStateCode.Normalize(State) ?? string.Empty,
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

    public UpdatePropertyDto ToUpdatePropertyDto(Property existingProperty, string propertyCode, ExternalPropertyIntakeContext context, Guid? owner1Id, Guid? owner2Id, Guid? owner3Id, Guid? propertyVendorContactId)
    {
        var updateDto = UpdatePropertyDto.FromProperty(existingProperty);
        updateDto.OrganizationId = context.OrganizationId;
        updateDto.PropertyId = existingProperty.PropertyId;
        updateDto.PropertyCode = propertyCode;
        updateDto.VendorId = propertyVendorContactId;
        updateDto.OfficeId = context.OfficeId;
        updateDto.PropertyLeaseTypeId = PropertyLeaseTypeId;
        updateDto.Owner1Id = owner1Id;
        updateDto.Owner2Id = owner2Id;
        updateDto.Owner3Id = owner3Id;

        if (IsActive.HasValue)
            updateDto.IsActive = IsActive.Value;

        if (MinStay.HasValue)
            updateDto.MinStay = MinStay.Value;

        if (MaxStay.HasValue)
            updateDto.MaxStay = MaxStay.Value;

        if (CheckInTimeId.HasValue)
            updateDto.CheckInTimeId = CheckInTimeId.Value;

        if (CheckOutTimeId.HasValue)
            updateDto.CheckOutTimeId = CheckOutTimeId.Value;

        updateDto.PropertyStyleId = PropertyStyleId;
        updateDto.PropertyTypeId = PropertyTypeId;
        updateDto.ExternalCalendars = PropertyICalDto.MergeCalendarInputs(ExternalCalendars, ExternalCalendar);
        updateDto.MonthlyRate = MonthlyRate;
        updateDto.DailyRate = DailyRate;
        updateDto.DepartureFee = DepartureFee ?? updateDto.DepartureFee;
        updateDto.MaidServiceFee = MaidServiceFee ?? updateDto.MaidServiceFee;
        updateDto.PetFee = PetFee ?? updateDto.PetFee;
        updateDto.Bedrooms = Bedrooms;
        updateDto.Bathrooms = Bathrooms;
        updateDto.Accommodates = Accommodates;
        updateDto.SquareFeet = SquareFeet;

        if (BedroomId1.HasValue)
            updateDto.BedroomId1 = BedroomId1.Value;

        if (BedroomId2.HasValue)
            updateDto.BedroomId2 = BedroomId2.Value;

        if (BedroomId3.HasValue)
            updateDto.BedroomId3 = BedroomId3.Value;

        if (BedroomId4.HasValue)
            updateDto.BedroomId4 = BedroomId4.Value;

        updateDto.Address1 = Address1.Trim();
        updateDto.Address2 = TrimOrNull(Address2);
        updateDto.Suite = TrimOrNull(Suite);
        updateDto.City = City.Trim();
        updateDto.State = UsStateCode.Normalize(State) ?? string.Empty;
        updateDto.Zip = Zip.Trim();
        updateDto.Neighborhood = TrimOrNull(Neighborhood);
        updateDto.CrossStreet = TrimOrNull(CrossStreet);
        updateDto.View = TrimOrNull(View);
        updateDto.Mailbox = TrimOrNull(Mailbox);
        updateDto.Unfurnished = Unfurnished ?? updateDto.Unfurnished;
        updateDto.Heating = Heating ?? updateDto.Heating;
        updateDto.Ac = Ac ?? updateDto.Ac;
        updateDto.Elevator = Elevator ?? updateDto.Elevator;
        updateDto.Security = Security ?? updateDto.Security;
        updateDto.Gated = Gated ?? updateDto.Gated;
        updateDto.PetsAllowed = PetsAllowed ?? updateDto.PetsAllowed;
        updateDto.DogsOkay = DogsOkay ?? updateDto.DogsOkay;
        updateDto.CatsOkay = CatsOkay ?? updateDto.CatsOkay;
        updateDto.PoundLimit = (PoundLimit ?? updateDto.PoundLimit).Trim();
        updateDto.Smoking = Smoking ?? updateDto.Smoking;
        updateDto.Parking = Parking ?? updateDto.Parking;
        updateDto.ParkingNotes = TrimOrNull(ParkingNotes) ?? updateDto.ParkingNotes;
        updateDto.Kitchen = Kitchen ?? updateDto.Kitchen;
        updateDto.Oven = Oven ?? updateDto.Oven;
        updateDto.Refrigerator = Refrigerator ?? updateDto.Refrigerator;
        updateDto.Microwave = Microwave ?? updateDto.Microwave;
        updateDto.Dishwasher = Dishwasher ?? updateDto.Dishwasher;
        updateDto.Bathtub = Bathtub ?? updateDto.Bathtub;
        updateDto.WasherDryerInUnit = WasherDryerInUnit ?? updateDto.WasherDryerInUnit;
        updateDto.WasherDryerInBldg = WasherDryerInBldg ?? updateDto.WasherDryerInBldg;
        updateDto.Tv = Tv ?? updateDto.Tv;
        updateDto.Cable = Cable ?? updateDto.Cable;
        updateDto.Dvd = Dvd ?? updateDto.Dvd;
        updateDto.Streaming = Streaming ?? updateDto.Streaming;
        updateDto.FastInternet = FastInternet ?? updateDto.FastInternet;
        updateDto.Deck = Deck ?? updateDto.Deck;
        updateDto.Patio = Patio ?? updateDto.Patio;
        updateDto.Yard = Yard ?? updateDto.Yard;
        updateDto.Garden = Garden ?? updateDto.Garden;
        updateDto.CommonPool = CommonPool ?? updateDto.CommonPool;
        updateDto.PrivatePool = PrivatePool ?? updateDto.PrivatePool;
        updateDto.Jacuzzi = Jacuzzi ?? updateDto.Jacuzzi;
        updateDto.Sauna = Sauna ?? updateDto.Sauna;
        updateDto.Gym = Gym ?? updateDto.Gym;
        updateDto.Amenities = TrimOrNull(Amenities) ?? updateDto.Amenities;
        updateDto.Description = Description.Trim();

        return updateDto;
    }

    private static void AddBedroomIdError(List<string> errors, int? bedroomId, string fieldName)
    {
        if (bedroomId.HasValue && !Enum.IsDefined(typeof(BedSizeType), bedroomId.Value))
            errors.Add($"{fieldName} must be 0-7 (0=Unknown, 1=King, 2=Queen, 3=Double, 4=Twin, 5=TwoTwins, 6=DayBed, 7=SofaBed). Received {bedroomId.Value}.");
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
