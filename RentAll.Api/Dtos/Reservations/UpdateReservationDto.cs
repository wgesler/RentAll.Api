using RentAll.Api.Dtos.ExtraFeeLines;
using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations;

public class UpdateReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public int ReservationNoticeId { get; set; }
    public int NumberOfPeople { get; set; }
    public string? TenantName { get; set; }
    public DateTimeOffset ArrivalDate { get; set; }
    public DateTimeOffset DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
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
    public DateTimeOffset MaidStartDate { get; set; }
    public decimal Taxes { get; set; }
    public string? Notes { get; set; }
    public List<UpdateExtraFeeLineDto> ExtraFeeLines { get; set; } = new List<UpdateExtraFeeLineDto>();
    public bool AllowExtensions { get; set; }
    public int CurrentInvoiceNumber { get; set; }
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

        if (ContactId == Guid.Empty)
            return (false, "ContactId is required");

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
            ContactId = ContactId,
            ReservationType = (ReservationType)ReservationTypeId,
            ReservationStatus = (ReservationStatus)ReservationStatusId,
            ReservationNotice = (ReservationNotice)ReservationNoticeId,
            NumberOfPeople = NumberOfPeople,
            TenantName = TenantName,
            ArrivalDate = ArrivalDate,
            DepartureDate = DepartureDate,
            CheckInTime = (CheckInTime)CheckInTimeId,
            CheckOutTime = (CheckOutTime)CheckOutTimeId,
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
            Taxes = Taxes,
            Notes = Notes,
            ExtraFeeLines = ExtraFeeLines?.Select(dto => dto.ToModel()).ToList() ?? new List<ExtraFeeLine>(),
            AllowExtensions = AllowExtensions,
            CurrentInvoiceNumber = CurrentInvoiceNumber,
            CreditDue = CreditDue,
            IsActive = IsActive,
            ModifiedBy = currentUser
        };
    }
}


