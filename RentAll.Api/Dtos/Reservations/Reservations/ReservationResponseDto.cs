using RentAll.Api.Dtos.Accounting.ExtraFeeLines;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationResponseDto
{
    public Guid ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string ReservationCode { get; set; } = string.Empty;
    public Guid? AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public List<Guid> ContactIds { get; set; } = new List<Guid>();
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
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
    public List<ExtraFeeLineResponseDto> ExtraFeeLines { get; set; } = new List<ExtraFeeLineResponseDto>();
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

    public int CurrentInvoiceNo { get; set; }
    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }


    public ReservationResponseDto(Reservation reservation)
    {
        ReservationId = reservation.ReservationId;
        OrganizationId = reservation.OrganizationId;
        OfficeId = reservation.OfficeId;
        OfficeName = reservation.OfficeName;
        ReservationCode = reservation.ReservationCode;
        AgentId = reservation.AgentId;
        PropertyId = reservation.PropertyId;
        ContactIds = reservation.ContactIds;
        ContactName = reservation.ContactName;
        CompanyId = reservation.CompanyId;
        CompanyName = reservation.CompanyName;
        ReservationTypeId = (int)reservation.ReservationType;
        ReservationStatusId = (int)reservation.ReservationStatus;
        ReservationNoticeId = (int)reservation.ReservationNotice;
        NumberOfPeople = reservation.NumberOfPeople;
        TenantName = reservation.TenantName;
        ReferenceNo = reservation.ReferenceNo;
        ArrivalDate = reservation.ArrivalDate;
        DepartureDate = reservation.DepartureDate;
        CheckInTimeId = (int)reservation.CheckInTime;
        CheckOutTimeId = (int)reservation.CheckOutTime;
        LockBoxCode = reservation.LockBoxCode;
        UnitTenantCode = reservation.UnitTenantCode;
        BillingMethodId = (int)reservation.BillingMethod;
        ProrateTypeId = (int)reservation.ProrateType;
        BillingTypeId = (int)reservation.BillingType;
        BillingRate = reservation.BillingRate;
        Deposit = reservation.Deposit;
        DepositTypeId = (int)reservation.DepositType;
        DepartureFee = reservation.DepartureFee;
        HasPets = reservation.HasPets;
        PetFee = reservation.PetFee;
        NumberOfPets = reservation.NumberOfPets;
        PetDescription = reservation.PetDescription;
        MaidService = reservation.MaidService;
        MaidServiceFee = reservation.MaidServiceFee;
        FrequencyId = (int)reservation.Frequency;
        MaidStartDate = reservation.MaidStartDate;
        MaidUserId = reservation.MaidUserId;
        Taxes = reservation.Taxes;
        Notes = reservation.Notes;
        ExtraFeeLines = reservation.ExtraFeeLines.Select(line => new ExtraFeeLineResponseDto(line)).ToList();
        AllowExtensions = reservation.AllowExtensions;
        PaymentReceived = reservation.PaymentReceived;
        WelcomeLetterChecked = reservation.WelcomeLetterChecked;
        WelcomeLetterSent = reservation.WelcomeLetterSent;
        ReadyForArrival = reservation.ReadyForArrival;
        Code = reservation.Code;
        DepartureLetterChecked = reservation.DepartureLetterChecked;
        DepartureLetterSent = reservation.DepartureLetterSent;
        aCleanerUserId = reservation.aCleanerUserId;
        aCleaningDate = reservation.aCleaningDate;
        aCarpetUserId = reservation.aCarpetUserId;
        aCarpetDate = reservation.aCarpetDate;
        aInspectorUserId = reservation.aInspectorUserId;
        aInspectingDate = reservation.aInspectingDate;
        dCleanerUserId = reservation.dCleanerUserId;
        dCleaningDate = reservation.dCleaningDate;
        dCarpetUserId = reservation.dCarpetUserId;
        dCarpetDate = reservation.dCarpetDate;
        dInspectorUserId = reservation.dInspectorUserId;
        dInspectingDate = reservation.dInspectingDate;
        CurrentInvoiceNo = reservation.CurrentInvoiceNo;
        CreditDue = reservation.CreditDue;
        IsActive = reservation.IsActive;
        CreatedOn = reservation.CreatedOn;
        CreatedBy = reservation.CreatedBy;
        ModifiedOn = reservation.ModifiedOn;
        ModifiedBy = reservation.ModifiedBy;
    }
}


