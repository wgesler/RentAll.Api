using RentAll.Api.Dtos.Accounting.ExtraFeeLines;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class UpdateReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public List<Guid> ContactIds { get; set; } = new List<Guid>();
    public Guid? CompanyId { get; set; }
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public int ReservationNoticeId { get; set; }
    public int NumberOfPeople { get; set; }
    public string? TenantName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
    public string? LockBoxCode { get; set; }
    public string? UnitTenantCode { get; set; }
    public int BillingMethodId { get; set; }
    public int ProrateTypeId { get; set; }
    public int BillingTypeId { get; set; }
    public decimal BillingRate { get; set; }
    public decimal Deposit { get; set; }
    public int DepositTypeId { get; set; }
    public decimal DepartureFee { get; set; }
    public bool HasPets { get; set; }
    public decimal PetFee { get; set; }
    public int NumberOfPets { get; set; }
    public string? PetDescription { get; set; }
    public bool MaidService { get; set; }
    public decimal MaidServiceFee { get; set; }
    public int FrequencyId { get; set; }
    public DateOnly MaidStartDate { get; set; }
    public Guid? MaidUserId { get; set; }
    public decimal Taxes { get; set; }
    public string? Notes { get; set; }
    public List<UpdateExtraFeeLineDto> ExtraFeeLines { get; set; } = new List<UpdateExtraFeeLineDto>();
    public bool AllowExtensions { get; set; }
    public bool PaymentReceived { get; set; }
    public bool WelcomeLetterChecked { get; set; }
    public bool WelcomeLetterSent { get; set; }
    public bool ReadyForArrival { get; set; }
    public bool Code { get; set; }
    public bool DepartureLetterChecked { get; set; }
    public bool DepartureLetterSent { get; set; }

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

    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (PropertyId == Guid.Empty)
            return (false, "PropertyId is required");

        if (string.IsNullOrWhiteSpace(ReservationCode))
            return (false, "ReservationCode is required");

        if (string.IsNullOrWhiteSpace(TenantName))
            return (false, "TenantName is required");

        if (ContactIds == null || !ContactIds.Any())
            return (false, "At least one ContactId is required");

        if (ContactIds.Any(id => id == Guid.Empty))
            return (false, "ContactId values must be non-empty");

        if (ArrivalDate >= DepartureDate)
            return (false, "DepartureDate must be after ArrivalDate");

        if (BillingRate < 0)
            return (false, "BillingRate must be zero or greater");

        if (NumberOfPeople < 0)
            return (false, "NumberOfPeople must be zero or greater");

        if (!Enum.IsDefined(typeof(ReservationType), ReservationTypeId))
            return (false, $"Invalid ReservationTypeId value: {ReservationTypeId}");

        if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId))
            return (false, $"Invalid ReservationStatusId value: {ReservationStatusId}");

        if (!Enum.IsDefined(typeof(ReservationNotice), ReservationNoticeId))
            return (false, $"Invalid ReservationNoticeId value: {ReservationNoticeId}");

        if (!Enum.IsDefined(typeof(CheckInTime), CheckInTimeId))
            return (false, $"Invalid CheckInTimeId value: {CheckInTimeId}");

        if (!Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId))
            return (false, $"Invalid CheckOutTimeId value: {CheckOutTimeId}");

        if (!Enum.IsDefined(typeof(BillingMethod), BillingMethodId))
            return (false, $"Invalid BillingMethodId value: {BillingMethodId}");

        if (!Enum.IsDefined(typeof(ProrateType), ProrateTypeId))
            return (false, $"Invalid ProrateTypeId value: {ProrateTypeId}");

        if (!Enum.IsDefined(typeof(BillingType), BillingTypeId))
            return (false, $"Invalid BillingTypeId value: {BillingTypeId}");

        if (!Enum.IsDefined(typeof(DepositType), DepositTypeId))
            return (false, $"Invalid DepositTypeId value: {DepositTypeId}");

        if (!Enum.IsDefined(typeof(FrequencyType), FrequencyId))
            return (false, $"Invalid FrequencyId value: {FrequencyId}");

        if (ExtraFeeLines != null)
        {
            foreach (var line in ExtraFeeLines)
            {
                var (isValid, errorMessage) = line.IsValid();
                if (!isValid)
                    return (false, $"ExtraFeeLine validation failed: {errorMessage}");
            }
        }

        return (true, null);
    }

    public Reservation ToModel(Guid currentUser)
    {
        return new Reservation
        {
            ReservationId = ReservationId,
            OrganizationId = OrganizationId,
            ReservationCode = ReservationCode,
            AgentId = AgentId,
            PropertyId = PropertyId,
            ContactIds = ContactIds,
            CompanyId = CompanyId,
            ReservationType = (ReservationType)ReservationTypeId,
            ReservationStatus = (ReservationStatus)ReservationStatusId,
            ReservationNotice = (ReservationNotice)ReservationNoticeId,
            NumberOfPeople = NumberOfPeople,
            TenantName = TenantName,
            ReferenceNo = ReferenceNo,
            ArrivalDate = ArrivalDate,
            DepartureDate = DepartureDate,
            CheckInTime = (CheckInTime)CheckInTimeId,
            CheckOutTime = (CheckOutTime)CheckOutTimeId,
            LockBoxCode = LockBoxCode,
            UnitTenantCode = UnitTenantCode,
            BillingMethod = (BillingMethod)BillingMethodId,
            ProrateType = (ProrateType)ProrateTypeId,
            BillingType = (BillingType)BillingTypeId,
            BillingRate = BillingRate,
            Deposit = Deposit,
            DepositType = (DepositType)DepositTypeId,
            DepartureFee = DepartureFee,
            HasPets = HasPets,
            PetFee = PetFee,
            NumberOfPets = NumberOfPets,
            PetDescription = PetDescription,
            MaidService = MaidService,
            MaidServiceFee = MaidServiceFee,
            Frequency = (FrequencyType)FrequencyId,
            MaidStartDate = MaidStartDate,
            MaidUserId = MaidUserId,
            Taxes = Taxes,
            Notes = Notes,
            ExtraFeeLines = ExtraFeeLines?.Select(dto => dto.ToModel()).ToList() ?? new List<ExtraFeeLine>(),
            AllowExtensions = AllowExtensions,
            PaymentReceived = PaymentReceived,
            WelcomeLetterChecked = WelcomeLetterChecked,
            WelcomeLetterSent = WelcomeLetterSent,
            ReadyForArrival = ReadyForArrival,
            Code = Code,
            DepartureLetterChecked = DepartureLetterChecked,
            DepartureLetterSent = DepartureLetterSent,
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
            CreditDue = CreditDue,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}


