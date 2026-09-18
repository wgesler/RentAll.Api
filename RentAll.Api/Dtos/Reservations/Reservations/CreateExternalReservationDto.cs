using RentAll.Api.Dtos.External;
using System.Text.Json;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class CreateExternalReservationDto
{
    public string PropertyCode { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public string? TenantName { get; set; }
    public string? AgentCode { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly? BillingStartDate { get; set; }
    public DateOnly? BillingEndDate { get; set; }
    public int? NumberOfPeople { get; set; }
    public int? ReservationTypeId { get; set; }
    public int? ReservationStatusId { get; set; }
    public int? ReservationNoticeId { get; set; }
    public int? CheckInTimeId { get; set; }
    public int? CheckOutTimeId { get; set; }
    public string? LockBoxCode { get; set; }
    public string? UnitTenantCode { get; set; }
    public string? GarageCode { get; set; }
    public int? BillingMethodId { get; set; }
    public int? BillingTypeId { get; set; }
    public int? ProrateTypeId { get; set; }
    public decimal? BillingRate { get; set; }
    public decimal? Deposit { get; set; }
    public int? DepositTypeId { get; set; }
    public decimal? DepartureFee { get; set; }
    public bool? HasPets { get; set; }
    public decimal? PetFee { get; set; }
    public int? NumberOfPets { get; set; }
    public string? PetDescription { get; set; }
    public bool? MaidService { get; set; }
    public decimal? MaidServiceFee { get; set; }
    public int? FrequencyId { get; set; }
    public DateOnly? MaidStartDate { get; set; }
    public string? MaidEmail { get; set; }
    public decimal? Taxes { get; set; }
    public string? Notes { get; set; }
    public bool? AllowExtensions { get; set; }
    public bool? BilledToEmployer { get; set; }
    public bool? CollapseCharges { get; set; }
    public int? InvoiceMethodId { get; set; }
    public bool? IsActive { get; set; }
    public Guid? aCleanerUserId { get; set; }
    public DateOnly? aCleaningDate { get; set; }
    public Guid? aCarpetUserId { get; set; }
    public DateOnly? aCarpetDate { get; set; }
    public Guid? aInspectorUserId { get; set; }
    public DateOnly? aInspectingDate { get; set; }
    public Guid? dCleanerUserId { get; set; }
    public DateOnly? dCleaningDate { get; set; }
    public Guid? dCarpetUserId { get; set; }
    public DateOnly? dCarpetDate { get; set; }
    public Guid? dInspectorUserId { get; set; }
    public DateOnly? dInspectingDate { get; set; }
    public ExternalPropertyContactDto? Contact { get; set; }
    public ExternalPropertyContactDto? Company { get; set; }
    public List<CreateExternalReservationExtraFeeDto> ExtraFeeLines { get; set; } = [];

    public bool ResolvedHasPets => HasPets ?? false;
    public int ResolvedDepositTypeId => DepositTypeId ?? (int)DepositType.Deposit;
    public bool RequiresCompany => ReservationTypeId is (int)ReservationType.Corporate or (int)ReservationType.Platform;
    public EntityType ExpectedContactEntityType => ReservationTypeId == (int)ReservationType.Owner ? EntityType.Owner : EntityType.Tenant;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        var errors = CollectErrors();
        return errors.Count == 0 ? (true, null) : (false, ExternalIntakeErrors.Join(errors));
    }

    public List<string> CollectErrors()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(PropertyCode))
            errors.Add("propertyCode is required.");
        if (string.IsNullOrWhiteSpace(TenantName))
            errors.Add("tenantName is required.");
        if (!ReservationTypeId.HasValue)
            errors.Add("reservationTypeId is required.");
        else if (!Enum.IsDefined(typeof(ReservationType), ReservationTypeId.Value))
            errors.Add($"reservationTypeId must be 0=Individual, 1=Corporate, 2=Owner, or 3=Platform. Received {ReservationTypeId.Value}.");
        if (!ReservationStatusId.HasValue)
            errors.Add("reservationStatusId is required.");
        else if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId.Value))
            errors.Add($"reservationStatusId must be 0=PreBooking through 8=Offline. Received {ReservationStatusId.Value}.");
        if (!ReservationNoticeId.HasValue)
            errors.Add("reservationNoticeId is required.");
        else if (!Enum.IsDefined(typeof(ReservationNotice), ReservationNoticeId.Value))
            errors.Add($"reservationNoticeId must be 0=ThirtyDays, 1=FifteenDays, 2=FourteenDays, 3=SixtyDays, or 4=FirmEndDate. Received {ReservationNoticeId.Value}.");
        if (ReservationTypeId != (int)ReservationType.Owner && string.IsNullOrWhiteSpace(AgentCode))
            errors.Add("agentCode is required.");
        if (ArrivalDate == default)
            errors.Add("arrivalDate is required.");
        if (DepartureDate == default)
            errors.Add("departureDate is required.");
        if (ArrivalDate != default && DepartureDate != default && ArrivalDate >= DepartureDate)
            errors.Add("departureDate must be after arrivalDate.");
        if (BillingStartDate.HasValue && BillingEndDate.HasValue && BillingStartDate.Value >= BillingEndDate.Value)
            errors.Add("billingEndDate must be after billingStartDate.");
        if (!NumberOfPeople.HasValue)
            errors.Add("numberOfPeople is required.");
        else if (NumberOfPeople.Value < 0)
            errors.Add("numberOfPeople must be zero or greater.");
        if (!BillingTypeId.HasValue)
            errors.Add("billingTypeId is required.");
        else if (!Enum.IsDefined(typeof(BillingType), BillingTypeId.Value))
            errors.Add($"billingTypeId must be 0=Monthly, 1=Daily, or 2=Nightly. Received {BillingTypeId.Value}.");
        if (!BillingRate.HasValue)
            errors.Add("billingRate is required.");
        else if (BillingRate.Value < 0)
            errors.Add("billingRate must be zero or greater.");
        if (!DepositTypeId.HasValue)
            errors.Add("depositTypeId is required.");
        else if (!Enum.IsDefined(typeof(DepositType), ResolvedDepositTypeId))
            errors.Add($"depositTypeId must be 0=Deposit, 1=CLR, or 2=SDW. Received {ResolvedDepositTypeId}.");
        else if (ResolvedDepositTypeId == (int)DepositType.CLR)
        {
            if (Deposit.HasValue && Deposit.Value != 0)
                errors.Add("deposit must be 0 when depositTypeId is 1=CLR.");
        }
        else if (!Deposit.HasValue)
            errors.Add("deposit is required.");
        else if (Deposit.Value < 0)
            errors.Add("deposit must be zero or greater.");
        if (!DepartureFee.HasValue)
            errors.Add("departureFee is required.");
        else if (DepartureFee.Value < 0)
            errors.Add("departureFee must be zero or greater.");
        if (CheckInTimeId.HasValue && !Enum.IsDefined(typeof(CheckInTime), CheckInTimeId.Value))
            errors.Add($"checkInTimeId must be 1=12PM through 6=5PM. Received {CheckInTimeId.Value}.");
        if (CheckOutTimeId.HasValue && !Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId.Value))
            errors.Add($"checkOutTimeId must be 1=8AM through 6=1PM. Received {CheckOutTimeId.Value}.");
        if (BillingMethodId.HasValue && !Enum.IsDefined(typeof(BillingMethod), BillingMethodId.Value))
            errors.Add($"billingMethodId must be 0=Invoice or 1=CreditCard. Received {BillingMethodId.Value}.");
        if (ProrateTypeId.HasValue && !Enum.IsDefined(typeof(ProrateType), ProrateTypeId.Value))
            errors.Add($"prorateTypeId must be 0=FirstMonth or 1=SecondMonth. Received {ProrateTypeId.Value}.");
        if (ResolvedHasPets)
        {
            if (!PetFee.HasValue)
                errors.Add("petFee is required when pets is true.");
            else if (PetFee.Value < 0)
                errors.Add("petFee must be zero or greater.");
            if (!NumberOfPets.HasValue)
                errors.Add("numberOfPets is required when pets is true.");
            else if (NumberOfPets.Value < 0)
                errors.Add("numberOfPets must be zero or greater.");
            if (string.IsNullOrWhiteSpace(PetDescription))
                errors.Add("petDescription is required when pets is true.");
        }
        if (MaidService == true)
        {
            if (!MaidServiceFee.HasValue)
                errors.Add("maidServiceFee is required when maidService is true.");
            else if (MaidServiceFee.Value < 0)
                errors.Add("maidServiceFee must be zero or greater.");
            if (!FrequencyId.HasValue)
                errors.Add("frequencyId is required when maidService is true.");
            else if (!Enum.IsDefined(typeof(FrequencyType), FrequencyId.Value))
                errors.Add($"frequencyId must be 0=NA through 8=Daily. Received {FrequencyId.Value}.");
            if (!MaidStartDate.HasValue)
                errors.Add("maidStartDate is required when maidService is true.");
        }
        else if (FrequencyId.HasValue && !Enum.IsDefined(typeof(FrequencyType), FrequencyId.Value))
            errors.Add($"frequencyId must be 0=NA through 8=Daily. Received {FrequencyId.Value}.");
        if (InvoiceMethodId.HasValue && !Enum.IsDefined(typeof(InvoiceMethod), InvoiceMethodId.Value))
            errors.Add($"invoiceMethodId must be 0=Create, 1=Download, 2=Email, or 3=Print. Received {InvoiceMethodId.Value}.");
        if (Contact == null)
            errors.Add("contact is required.");
        else
            errors.AddRange(ValidateContact(Contact, "contact", ExpectedContactEntityType));
        if (RequiresCompany && Company == null)
            errors.Add("company is required when reservationTypeId is 1=Corporate or 3=Platform.");
        if (Company != null)
            errors.AddRange(ValidateContact(Company, "company", EntityType.Company));
        for (var index = 0; index < ExtraFeeLines.Count; index++)
            errors.AddRange(ExtraFeeLines[index].CollectErrors($"extraFeeLines[{index}]"));

        return errors;
    }

    public static List<string> CollectFromJson(JsonElement body, string prefix)
    {
        var errors = new List<string>();
        if (body.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{prefix} must be an object. Received {ExternalIntakeErrors.Describe(body)}.");
            return errors;
        }

        ExternalIntakeErrors.CollectRequiredString(body, "propertyCode", errors, $"{prefix}.propertyCode is required.");
        ExternalIntakeErrors.CollectRequiredString(body, "tenantName", errors, $"{prefix}.tenantName is required.");
        ExternalIntakeErrors.CollectRequiredInt(body, "reservationTypeId", errors, $"{prefix}.reservationTypeId is required.");
        ExternalIntakeErrors.CollectRequiredInt(body, "reservationStatusId", errors, $"{prefix}.reservationStatusId is required.");
        ExternalIntakeErrors.CollectRequiredInt(body, "reservationNoticeId", errors, $"{prefix}.reservationNoticeId is required.");
        CollectRequiredAgent(body, prefix, errors);
        ExternalIntakeErrors.CollectRequiredDateOnly(body, "arrivalDate", errors, $"{prefix}.arrivalDate is required.");
        ExternalIntakeErrors.CollectRequiredDateOnly(body, "departureDate", errors, $"{prefix}.departureDate is required.");
        ExternalIntakeErrors.CollectRequiredNonNegativeInt(body, "numberOfPeople", errors, $"{prefix}.numberOfPeople is required.");
        ExternalIntakeErrors.CollectRequiredInt(body, "billingTypeId", errors, $"{prefix}.billingTypeId is required.");
        ExternalIntakeErrors.CollectRequiredNonNegativeDecimal(body, "billingRate", errors, $"{prefix}.billingRate is required.");
        ExternalIntakeErrors.CollectRequiredInt(body, "depositTypeId", errors, $"{prefix}.depositTypeId is required.");
        CollectDeposit(body, prefix, errors);
        ExternalIntakeErrors.CollectRequiredNonNegativeDecimal(body, "departureFee", errors, $"{prefix}.departureFee is required.");
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "billingStartDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "billingEndDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "aCleaningDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "aCarpetDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "aInspectingDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "dCleaningDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "dCarpetDate", errors);
        ExternalIntakeErrors.CollectOptionalDateOnly(body, "dInspectingDate", errors);
        ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(body, "taxes", errors);
        ExternalIntakeErrors.CollectOptionalBools(body, ["hasPets", "maidService", "allowExtensions", "billedToEmployer", "collapseCharges", "isActive"], errors);
        CollectPetFields(body, prefix, errors);
        CollectMaidFields(body, prefix, errors);
        CollectRequiredContact(body, prefix, "contact", errors);
        if (RequiresCompanyFromJson(body))
            CollectRequiredContact(body, prefix, "company", errors);
        else
            CollectOptionalContact(body, prefix, "company", errors);
        CollectExtraFeeLines(body, prefix, errors);

        return errors.Select(error => error.StartsWith(prefix, StringComparison.Ordinal) ? error : $"{prefix}.{error}").ToList();
    }

    public CreateReservationDto ToCreateReservationDto(
        Guid organizationId,
        int officeId,
        Guid propertyId,
        Property property,
        IReadOnlyList<Guid> contactIds,
        Guid? companyId,
        Guid? agentId,
        Guid? maidUserId)
    {
        var billingTypeId = BillingTypeId!.Value;
        var hasPets = ResolvedHasPets;
        var maidService = MaidService ?? false;
        var reservationTypeId = ReservationTypeId!.Value;

        return new CreateReservationDto
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            AgentId = reservationTypeId == (int)ReservationType.Owner ? null : agentId,
            PropertyId = propertyId,
            ContactIds = contactIds.ToList(),
            CompanyId = companyId,
            ReservationTypeId = reservationTypeId,
            ReservationStatusId = ReservationStatusId!.Value,
            ReservationNoticeId = ReservationNoticeId!.Value,
            NumberOfPeople = NumberOfPeople!.Value,
            TenantName = TenantName!.Trim(),
            ReferenceNo = string.IsNullOrWhiteSpace(ReferenceNo) ? null : ReferenceNo.Trim(),
            ArrivalDate = ArrivalDate,
            DepartureDate = DepartureDate,
            BillingStartDate = BillingStartDate,
            BillingEndDate = BillingEndDate,
            CheckInTimeId = CheckInTimeId ?? (int)CheckInTime.FourPM,
            CheckOutTimeId = CheckOutTimeId ?? (int)CheckOutTime.ElevenAM,
            LockBoxCode = TrimOrNull(LockBoxCode),
            UnitTenantCode = TrimOrNull(UnitTenantCode),
            GarageCode = TrimOrNull(GarageCode),
            BillingMethodId = BillingMethodId ?? (int)BillingMethod.Invoice,
            ProrateTypeId = ProrateTypeId ?? (int)ProrateType.FirstMonth,
            BillingTypeId = billingTypeId,
            BillingRate = BillingRate!.Value,
            Deposit = ResolvedDepositTypeId == (int)DepositType.CLR ? 0 : Deposit!.Value,
            DepositTypeId = ResolvedDepositTypeId,
            DepositReturned = false,
            DepartureFee = DepartureFee!.Value,
            HasPets = hasPets,
            PetFee = hasPets ? PetFee!.Value : 0,
            NumberOfPets = hasPets ? NumberOfPets!.Value : 0,
            PetDescription = hasPets ? PetDescription : null,
            MaidService = maidService,
            MaidServiceFee = maidService ? MaidServiceFee!.Value : 0,
            FrequencyId = maidService ? FrequencyId!.Value : (int)FrequencyType.NA,
            MaidStartDate = maidService ? MaidStartDate!.Value : ArrivalDate,
            MaidUserId = maidUserId,
            Taxes = Taxes ?? 0,
            Notes = Notes ?? string.Empty,
            AllowExtensions = AllowExtensions ?? true,
            BilledToEmployer = reservationTypeId == (int)ReservationType.Corporate && (BilledToEmployer ?? false),
            CollapseCharges = CollapseCharges ?? false,
            InvoiceMethodId = InvoiceMethodId ?? (int)InvoiceMethod.Create,
            aCleanerUserId = aCleanerUserId,
            aCleaningDate = aCleaningDate,
            aCarpetUserId = aCarpetUserId,
            aCarpetDate = aCarpetDate,
            aInspectorUserId = aInspectorUserId,
            aInspectingDate = aInspectingDate,
            dCleanerUserId = dCleanerUserId,
            dCleaningDate = dCleaningDate,
            dCarpetUserId = dCarpetUserId,
            dCarpetDate = dCarpetDate,
            dInspectorUserId = dInspectorUserId,
            dInspectingDate = dInspectingDate,
            IsActive = IsActive ?? true
        };
    }

    public void ApplyToExisting(
        Reservation reservation,
        Property property,
        IReadOnlyList<Guid> contactIds,
        Guid? companyId,
        Guid? agentId,
        Guid? maidUserId)
    {
        var create = ToCreateReservationDto(reservation.OrganizationId, reservation.OfficeId, property.PropertyId, property, contactIds, companyId, agentId, maidUserId);
        reservation.AgentId = create.AgentId;
        reservation.ContactIds = create.ContactIds;
        reservation.CompanyId = create.CompanyId;
        reservation.ReservationType = (ReservationType)create.ReservationTypeId;
        reservation.ReservationStatus = (ReservationStatus)create.ReservationStatusId;
        reservation.ReservationNotice = (ReservationNotice)create.ReservationNoticeId;
        reservation.NumberOfPeople = create.NumberOfPeople;
        reservation.TenantName = create.TenantName;
        reservation.ReferenceNo = create.ReferenceNo;
        reservation.ArrivalDate = create.ArrivalDate;
        reservation.DepartureDate = create.DepartureDate;
        reservation.BillingStartDate = create.BillingStartDate;
        reservation.BillingEndDate = create.BillingEndDate;
        reservation.CheckInTime = (CheckInTime)create.CheckInTimeId;
        reservation.CheckOutTime = (CheckOutTime)create.CheckOutTimeId;
        reservation.LockBoxCode = create.LockBoxCode;
        reservation.UnitTenantCode = create.UnitTenantCode;
        reservation.GarageCode = create.GarageCode;
        reservation.BillingMethod = (BillingMethod)create.BillingMethodId;
        reservation.ProrateType = (ProrateType)create.ProrateTypeId;
        reservation.BillingType = (BillingType)create.BillingTypeId;
        reservation.BillingRate = create.BillingRate;
        reservation.Deposit = create.Deposit;
        reservation.DepositType = (DepositType)create.DepositTypeId;
        reservation.DepartureFee = create.DepartureFee;
        reservation.HasPets = create.HasPets;
        reservation.PetFee = create.PetFee;
        reservation.NumberOfPets = create.NumberOfPets;
        reservation.PetDescription = create.PetDescription;
        reservation.MaidService = create.MaidService;
        reservation.MaidServiceFee = create.MaidServiceFee;
        reservation.Frequency = (FrequencyType)create.FrequencyId;
        reservation.MaidStartDate = create.MaidStartDate;
        reservation.MaidUserId = create.MaidUserId;
        reservation.Taxes = create.Taxes;
        reservation.Notes = create.Notes;
        reservation.ExtraFeeLines = ExtraFeeLines.Select(line => line.ToModel()).ToList();
        reservation.AllowExtensions = create.AllowExtensions;
        reservation.BilledToEmployer = create.BilledToEmployer;
        reservation.CollapseCharges = create.CollapseCharges;
        reservation.InvoiceMethod = (InvoiceMethod)create.InvoiceMethodId;
        reservation.aCleanerUserId = create.aCleanerUserId;
        reservation.aCleaningDate = create.aCleaningDate;
        reservation.aCarpetUserId = create.aCarpetUserId;
        reservation.aCarpetDate = create.aCarpetDate;
        reservation.aInspectorUserId = create.aInspectorUserId;
        reservation.aInspectingDate = create.aInspectingDate;
        reservation.dCleanerUserId = create.dCleanerUserId;
        reservation.dCleaningDate = create.dCleaningDate;
        reservation.dCarpetUserId = create.dCarpetUserId;
        reservation.dCarpetDate = create.dCarpetDate;
        reservation.dInspectorUserId = create.dInspectorUserId;
        reservation.dInspectingDate = create.dInspectingDate;
        reservation.IsActive = create.IsActive;
    }

    public List<ExtraFeeLine> ToExtraFeeLines() => ExtraFeeLines.Select(line => line.ToModel()).ToList();

    private static List<string> ValidateContact(ExternalPropertyContactDto contact, string fieldLabel, EntityType expectedType)
    {
        if (contact.EntityTypeId == 0)
            contact.EntityTypeId = (int)expectedType;
        var errors = new List<string>();
        if (contact.EntityTypeId != (int)expectedType)
            errors.Add($"{fieldLabel}.entityTypeId must be {(int)expectedType}={expectedType}.");
        errors.AddRange(contact.CollectErrors(fieldLabel));
        return errors;
    }

    private static void CollectRequiredAgent(JsonElement body, string prefix, List<string> errors)
    {
        if (IsOwnerReservation(body) || ExternalIntakeErrors.HasProperty(body, "agentCode"))
            return;

        errors.Add($"{prefix}.agentCode is required.");
    }

    private static void CollectDeposit(JsonElement body, string prefix, List<string> errors)
    {
        if (IsClrDeposit(body))
        {
            ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(body, "deposit", errors);
            return;
        }

        ExternalIntakeErrors.CollectRequiredNonNegativeDecimal(body, "deposit", errors, $"{prefix}.deposit is required.");
    }

    private static bool IsOwnerReservation(JsonElement body) =>
        TryGetIntProperty(body, "reservationTypeId", out var typeId) && typeId == (int)ReservationType.Owner;

    private static bool RequiresCompanyFromJson(JsonElement body) =>
        TryGetIntProperty(body, "reservationTypeId", out var typeId)
        && typeId is (int)ReservationType.Corporate or (int)ReservationType.Platform;

    private static bool IsClrDeposit(JsonElement body) =>
        TryGetIntProperty(body, "depositTypeId", out var depositTypeId)
        && depositTypeId == (int)DepositType.CLR;

    private static bool TryGetIntProperty(JsonElement body, string field, out int value)
    {
        value = 0;
        if (!ExternalIntakeErrors.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return false;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        return element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString()?.Trim(), out value);
    }

    private static void CollectPetFields(JsonElement body, string prefix, List<string> errors)
    {
        var hasPets = ExternalIntakeErrors.TryGetOptionalBool(body, "hasPets", out var petsFlag) && petsFlag;
        if (!hasPets)
        {
            ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(body, "petFee", errors);
            ExternalIntakeErrors.CollectOptionalNonNegativeInt(body, "numberOfPets", errors);
            return;
        }

        ExternalIntakeErrors.CollectRequiredNonNegativeDecimal(body, "petFee", errors, $"{prefix}.petFee is required when pets is true.");
        ExternalIntakeErrors.CollectRequiredNonNegativeInt(body, "numberOfPets", errors, $"{prefix}.numberOfPets is required when pets is true.");
        ExternalIntakeErrors.CollectRequiredString(body, "petDescription", errors, $"{prefix}.petDescription is required when pets is true.");
    }

    private static void CollectMaidFields(JsonElement body, string prefix, List<string> errors)
    {
        var maidService = ExternalIntakeErrors.TryGetOptionalBool(body, "maidService", out var maidFlag) && maidFlag;
        if (!maidService)
        {
            ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(body, "maidServiceFee", errors);
            ExternalIntakeErrors.CollectOptionalDateOnly(body, "maidStartDate", errors);
            return;
        }

        ExternalIntakeErrors.CollectRequiredNonNegativeDecimal(body, "maidServiceFee", errors, $"{prefix}.maidServiceFee is required when maidService is true.");
        ExternalIntakeErrors.CollectRequiredInt(body, "frequencyId", errors, $"{prefix}.frequencyId is required when maidService is true.");
        ExternalIntakeErrors.CollectRequiredDateOnly(body, "maidStartDate", errors, $"{prefix}.maidStartDate is required when maidService is true.");
    }

    private static void CollectRequiredContact(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalIntakeErrors.TryGetProperty(body, field, out var contact) || contact.ValueKind == JsonValueKind.Null)
        {
            errors.Add($"{prefix}.{field} is required.");
            return;
        }

        CollectContactObject(contact, $"{prefix}.{field}", errors);
    }

    private static void CollectOptionalContact(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalIntakeErrors.TryGetProperty(body, field, out var contact) || contact.ValueKind == JsonValueKind.Null)
            return;

        CollectContactObject(contact, $"{prefix}.{field}", errors);
    }

    private static void CollectContactObject(JsonElement contact, string prefix, List<string> errors)
    {
        if (contact.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{prefix} must be an object. Received {ExternalIntakeErrors.Describe(contact)}.");
            return;
        }

        ExternalIntakeErrors.CollectRequiredString(contact, "firstName", errors, $"{prefix}.firstName is required.");
        ExternalIntakeErrors.CollectRequiredString(contact, "lastName", errors, $"{prefix}.lastName is required.");
        ExternalIntakeErrors.CollectRequiredEmail(contact, "email", errors, $"{prefix}.email is required.", $"{prefix}.email format is invalid.");
    }

    private static void CollectExtraFeeLines(JsonElement body, string prefix, List<string> errors)
    {
        if (!ExternalIntakeErrors.TryGetProperty(body, "extraFeeLines", out var lines) || lines.ValueKind == JsonValueKind.Null)
            return;

        if (lines.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{prefix}.extraFeeLines must be an array. Received {ExternalIntakeErrors.Describe(lines)}.");
            return;
        }

        var index = 0;
        foreach (var line in lines.EnumerateArray())
        {
            var linePrefix = $"{prefix}.extraFeeLines[{index}]";
            if (line.ValueKind != JsonValueKind.Object)
                errors.Add($"{linePrefix} must be an object. Received {ExternalIntakeErrors.Describe(line)}.");
            else
            {
                ExternalIntakeErrors.CollectRequiredString(line, "feeDescription", errors, $"{linePrefix}.feeDescription is required.");
                ExternalIntakeErrors.CollectOptionalNonNegativeDecimal(line, "feeAmount", errors);
            }

            index++;
        }
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
