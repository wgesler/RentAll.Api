namespace RentAll.Infrastructure.Entities.Reservations;

public class ReservationEntity
{
    public Guid ReservationId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public Guid? AgentId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public int ReservationNoticeId { get; set; }
    public string ContactIds { get; set; } = "[]";
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; } = string.Empty;
    public int NumberOfPeople { get; set; }
    public string? TenantName { get; set; }
    public string? ReferenceNo { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
    public string? LockBoxCode { get; set; }
    public string? UnitTenantCode { get; set; }

    // Billing Fields
    public int BillingMethodId { get; set; }
    public int ProrateTypeId { get; set; }
    public int BillingTypeId { get; set; }
    public decimal BillingRate { get; set; }
    public decimal Deposit { get; set; }
    public int DepositTypeId { get; set; }
    public decimal DepartureFee { get; set; }

    // Pets and Maids
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

    // Payment Fields
    public string? ExtraFeeLines { get; set; }
    public bool AllowExtensions { get; set; }
    public int CurrentInvoiceNo { get; set; }
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

    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

}
