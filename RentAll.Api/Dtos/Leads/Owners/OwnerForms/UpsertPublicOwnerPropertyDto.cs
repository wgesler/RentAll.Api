using RentAll.Domain.Models.Leads;
using System.Globalization;

namespace RentAll.Api.Dtos.Leads.Owners;

public class UpsertPublicOwnerPropertyDto
{
    public string? OrganizationId { get; set; }
    public string? PropertyCode { get; set; }
    public int? PropertyLeaseTypeId { get; set; }
    public string? Owner1Id { get; set; }
    public string? Owner2Id { get; set; }
    public string? Owner3Id { get; set; }
    public string? VendorId { get; set; }
    public bool? IsActive { get; set; }

    public string? AvailableFrom { get; set; }
    public string? AvailableUntil { get; set; }
    public int? MinStay { get; set; }
    public int? MaxStay { get; set; }
    public int? CheckInTimeId { get; set; }
    public int? CheckOutTimeId { get; set; }
    public int? PropertyStyleId { get; set; }
    public int? PropertyTypeId { get; set; }
    public int? PropertyStatusId { get; set; }
    public int? NoticeToVacateId { get; set; }
    public int? OfficeId { get; set; }
    public int? BuildingId { get; set; }
    public int? RegionId { get; set; }
    public int? AreaId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? MonthlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public decimal? DepartureFee { get; set; }
    public decimal? MaidServiceFee { get; set; }
    public decimal? PetFee { get; set; }
    public decimal? ExtraFee { get; set; }
    public string? ExtraFeeName { get; set; }
    public string? BldgNo { get; set; }
    public int? UnitLevel { get; set; }
    public int? Bedrooms { get; set; }
    public decimal? Bathrooms { get; set; }
    public int? Accomodates { get; set; }
    public int? SquareFeet { get; set; }
    public int? BedroomId1 { get; set; }
    public int? BedroomId2 { get; set; }
    public int? BedroomId3 { get; set; }
    public int? BedroomId4 { get; set; }
    public int? Sofabed { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Suite { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string? CommunityAddress { get; set; }
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
    public string? AlarmCode { get; set; }
    public string? UnitMstrCode { get; set; }
    public string? BldgMstrCode { get; set; }
    public string? BldgTenantCode { get; set; }
    public string? MailRoomCode { get; set; }
    public string? GarageCode { get; set; }
    public string? GateCode { get; set; }
    public string? TrashCode { get; set; }
    public string? StorageCode { get; set; }

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
    public string? InternetNetwork { get; set; }
    public string? InternetPassword { get; set; }
    public bool? Deck { get; set; }
    public bool? Patio { get; set; }
    public bool? Yard { get; set; }
    public bool? Garden { get; set; }
    public bool? CommonPool { get; set; }
    public bool? PrivatePool { get; set; }
    public bool? Jacuzzi { get; set; }
    public bool? Sauna { get; set; }
    public bool? Gym { get; set; }
    public int? TrashPickupId { get; set; }
    public string? TrashRemoval { get; set; }
    public string? Amenities { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public string? OnCleanerUserId { get; set; }
    public string? OnCleaningDate { get; set; }
    public string? OnCarpetUserId { get; set; }
    public string? OnCarpetDate { get; set; }
    public string? OnInspectorUserId { get; set; }
    public string? OnInspectingDate { get; set; }
    public string? OffCleanerUserId { get; set; }
    public string? OffCleaningDate { get; set; }
    public string? OffCarpetUserId { get; set; }
    public string? OffCarpetDate { get; set; }
    public string? OffInspectorUserId { get; set; }
    public string? OffInspectingDate { get; set; }
    public bool? OnlineChecked { get; set; }
    public bool? OfflineChecked { get; set; }

    public CreatePropertyDto ToCreatePropertyDto(LeadOwner owner, Guid? ownerContactId)
    {
        var propertyCode = ResolveRequired(PropertyCode, owner.PropertyCode);
        var officeId = OfficeId.HasValue && OfficeId.Value > 0 ? OfficeId.Value : owner.OfficeId;

        return new CreatePropertyDto
        {
            OrganizationId = ParseGuid(OrganizationId) ?? owner.OrganizationId,
            PropertyCode = propertyCode,
            PropertyLeaseTypeId = PropertyLeaseTypeId ?? (int)PropertyLeaseType.PropertyManagement,
            Owner1Id = ParseGuid(Owner1Id) ?? ownerContactId,
            Owner2Id = ParseGuid(Owner2Id),
            Owner3Id = ParseGuid(Owner3Id),
            VendorId = ParseGuid(VendorId),
            IsActive = IsActive ?? true,
            AvailableFrom = ParseDateOnly(AvailableFrom),
            AvailableUntil = ParseDateOnly(AvailableUntil),
            MinStay = MinStay ?? 0,
            MaxStay = MaxStay ?? 0,
            CheckInTimeId = CheckInTimeId ?? (int)CheckInTime.FourPM,
            CheckOutTimeId = CheckOutTimeId ?? (int)CheckOutTime.ElevenAM,
            PropertyStyleId = PropertyStyleId ?? (int)PropertyStyle.Standard,
            PropertyTypeId = PropertyTypeId ?? (int)PropertyType.Unspecified,
            PropertyStatusId = PropertyStatusId ?? (int)PropertyStatus.Vacant,
            NoticeToVacateId = NoticeToVacateId ?? (int)ReservationNotice.ThirtyDays,
            OfficeId = officeId,
            BuildingId = BuildingId,
            RegionId = RegionId,
            AreaId = AreaId,
            Latitude = Latitude,
            Longitude = Longitude,
            MonthlyRate = MonthlyRate ?? 0,
            DailyRate = DailyRate ?? 0,
            DepartureFee = DepartureFee ?? 0,
            MaidServiceFee = MaidServiceFee ?? 0,
            PetFee = PetFee ?? 0,
            ExtraFee = ExtraFee ?? 0,
            ExtraFeeName = (ExtraFeeName ?? string.Empty).Trim(),
            BldgNo = TrimOrNull(BldgNo),
            UnitLevel = UnitLevel ?? 1,
            Bedrooms = Bedrooms ?? 0,
            Bathrooms = Bathrooms ?? 1,
            Accomodates = Accomodates ?? 0,
            SquareFeet = SquareFeet ?? 0,
            BedroomId1 = BedroomId1 ?? 0,
            BedroomId2 = BedroomId2 ?? 0,
            BedroomId3 = BedroomId3 ?? 0,
            BedroomId4 = BedroomId4 ?? 0,
            Sofabed = Sofabed ?? 0,
            Address1 = ResolveRequired(Address1, owner.Address),
            Address2 = TrimOrNull(Address2),
            Suite = TrimOrNull(Suite),
            City = ResolveRequired(City, owner.City),
            State = ResolveRequired(State, owner.State),
            Zip = ResolveRequired(Zip, owner.Zip),
            Phone = TrimOrNull(Phone),
            CommunityAddress = TrimOrNull(CommunityAddress),
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
            AlarmCode = TrimOrNull(AlarmCode),
            UnitMstrCode = TrimOrNull(UnitMstrCode),
            BldgMstrCode = TrimOrNull(BldgMstrCode),
            BldgTenantCode = TrimOrNull(BldgTenantCode),
            MailRoomCode = TrimOrNull(MailRoomCode),
            GarageCode = TrimOrNull(GarageCode),
            GateCode = TrimOrNull(GateCode),
            TrashCode = TrimOrNull(TrashCode),
            StorageCode = TrimOrNull(StorageCode),
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
            InternetNetwork = TrimOrNull(InternetNetwork),
            InternetPassword = TrimOrNull(InternetPassword),
            Deck = Deck ?? false,
            Patio = Patio ?? false,
            Yard = Yard ?? false,
            Garden = Garden ?? false,
            CommonPool = CommonPool ?? false,
            PrivatePool = PrivatePool ?? false,
            Jacuzzi = Jacuzzi ?? false,
            Sauna = Sauna ?? false,
            Gym = Gym ?? false,
            TrashPickupId = TrashPickupId ?? 0,
            TrashRemoval = TrimOrNull(TrashRemoval),
            Amenities = TrimOrNull(Amenities),
            Description = TrimOrNull(Description),
            Notes = TrimOrNull(Notes),
            onCleanerUserId = ParseGuid(OnCleanerUserId),
            onCleaningDate = ParseDateOnly(OnCleaningDate),
            onCarpetUserId = ParseGuid(OnCarpetUserId),
            onCarpetDate = ParseDateOnly(OnCarpetDate),
            onInspectorUserId = ParseGuid(OnInspectorUserId),
            onInspectingDate = ParseDateOnly(OnInspectingDate),
            offCleanerUserId = ParseGuid(OffCleanerUserId),
            offCleaningDate = ParseDateOnly(OffCleaningDate),
            offCarpetUserId = ParseGuid(OffCarpetUserId),
            offCarpetDate = ParseDateOnly(OffCarpetDate),
            offInspectorUserId = ParseGuid(OffInspectorUserId),
            offInspectingDate = ParseDateOnly(OffInspectingDate),
            OnlineChecked = OnlineChecked ?? false,
            OfflineChecked = OfflineChecked ?? false
        };
    }

    private static Guid? ParseGuid(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return Guid.TryParse(trimmed, out var parsed) ? parsed : null;
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var asDateTime))
            return DateOnly.FromDateTime(asDateTime);

        return null;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string ResolveRequired(string? preferred, string? fallback)
    {
        var resolved = TrimOrNull(preferred) ?? TrimOrNull(fallback);
        return resolved ?? string.Empty;
    }
}
