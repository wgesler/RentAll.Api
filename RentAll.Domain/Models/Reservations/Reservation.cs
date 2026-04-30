using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Reservation
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
    public string? CompanyName { get; set; } = string.Empty;
    public ReservationType ReservationType { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public ReservationNotice ReservationNotice { get; set; }
    public int NumberOfPeople { get; set; }
    public string? TenantName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public CheckInTime CheckInTime { get; set; }
    public CheckOutTime CheckOutTime { get; set; }
    public string? LockBoxCode { get; set; }
    public string? UnitTenantCode { get; set; }
    public BillingMethod BillingMethod { get; set; }
    public ProrateType ProrateType { get; set; }
    public BillingType BillingType { get; set; }
    public decimal BillingRate { get; set; }
    public decimal Deposit { get; set; }
    public DepositType DepositType { get; set; }
    public decimal DepartureFee { get; set; }
    public bool HasPets { get; set; }
    public decimal PetFee { get; set; }
    public int NumberOfPets { get; set; }
    public string? PetDescription { get; set; }
    public bool MaidService { get; set; }
    public decimal MaidServiceFee { get; set; }
    public FrequencyType Frequency { get; set; }
    public DateOnly MaidStartDate { get; set; }
    public Guid? MaidUserId { get; set; }
    public decimal Taxes { get; set; }
    public string? Notes { get; set; }
    public List<ExtraFeeLine> ExtraFeeLines { get; set; } = new List<ExtraFeeLine>();
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
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

}


