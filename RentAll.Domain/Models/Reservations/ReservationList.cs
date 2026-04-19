using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ReservationList
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string? AgentCode { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public bool HasPets { get; set; }
    public Guid? MaidUserId { get; set; }
    public DateOnly? MaidStartDate { get; set; }
    public FrequencyType Frequency { get; set; }
    public decimal MaidServiceFee { get; set; }
    public bool PaymentReceived { get; set; }
    public bool WelcomeLetterChecked { get; set; }
    public bool WelcomeLetterSent { get; set; }
    public bool ReadyForArrival { get; set; }
    public bool Code { get; set; }
    public bool DepartureLetterChecked { get; set; }
    public bool DepartureLetterSent { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    // Arrival Service Providers
    public Guid? ArrivalCleanerUserId { get; set; }
    public DateOnly? ArrivalCleaningDate { get; set; }
    public Guid? ArrivalCarpetUserId { get; set; }
    public DateOnly? ArrivalCarpetDate { get; set; }
    public Guid? ArrivalInspectorUserId { get; set; }
    public DateOnly? ArrivalInspectingDate { get; set; }

    // Departure Service Providers
    public Guid? DepartureCleanerUserId { get; set; }
    public DateOnly? DepartureCleaningDate { get; set; }
    public Guid? DepartureCarpetUserId { get; set; }
    public DateOnly? DepartureCarpetDate { get; set; }
    public Guid? DepartureInspectorUserId { get; set; }
    public DateOnly? DepartureInspectingDate { get; set; }
}

