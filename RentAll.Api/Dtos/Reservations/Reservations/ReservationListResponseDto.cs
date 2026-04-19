namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationListResponseDto
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
    public int ReservationStatusId { get; set; }
    public bool HasPets { get; set; }
    public Guid? MaidUserId { get; set; }
    public DateOnly? MaidStartDate { get; set; }
    public int FrequencyId { get; set; }
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

    public ReservationListResponseDto(ReservationList reservationList)
    {
        ReservationId = reservationList.ReservationId;
        ReservationCode = reservationList.ReservationCode;
        PropertyId = reservationList.PropertyId;
        PropertyCode = reservationList.PropertyCode;
        OfficeId = reservationList.OfficeId;
        OfficeName = reservationList.OfficeName;
        ContactId = reservationList.ContactId;
        ContactName = reservationList.ContactName;
        CompanyId = reservationList.CompanyId;
        TenantName = reservationList.TenantName;
        CompanyName = reservationList.CompanyName;
        AgentCode = reservationList.AgentCode;
        MonthlyRate = reservationList.MonthlyRate;
        DailyRate = reservationList.DailyRate;
        ArrivalDate = reservationList.ArrivalDate;
        DepartureDate = reservationList.DepartureDate;
        ReservationStatusId = (int)reservationList.ReservationStatus;
        CurrentInvoiceNo = reservationList.CurrentInvoiceNo;
        HasPets = reservationList.HasPets;
        MaidUserId = reservationList.MaidUserId;
        MaidStartDate = reservationList.MaidStartDate;
        FrequencyId = (int)reservationList.Frequency;
        MaidServiceFee = reservationList.MaidServiceFee;
        PaymentReceived = reservationList.PaymentReceived;
        WelcomeLetterChecked = reservationList.WelcomeLetterChecked;
        WelcomeLetterSent = reservationList.WelcomeLetterSent;
        ReadyForArrival = reservationList.ReadyForArrival;
        Code = reservationList.Code;
        DepartureLetterChecked = reservationList.DepartureLetterChecked;
        DepartureLetterSent = reservationList.DepartureLetterSent;
        CreditDue = reservationList.CreditDue;
        IsActive = reservationList.IsActive;
        CreatedOn = reservationList.CreatedOn;
        ArrivalCleanerUserId = reservationList.ArrivalCleanerUserId;
        ArrivalCleaningDate = reservationList.ArrivalCleaningDate;
        ArrivalCarpetUserId = reservationList.ArrivalCarpetUserId;
        ArrivalCarpetDate = reservationList.ArrivalCarpetDate;
        ArrivalInspectorUserId = reservationList.ArrivalInspectorUserId;
        ArrivalInspectingDate = reservationList.ArrivalInspectingDate;
        DepartureCleanerUserId = reservationList.DepartureCleanerUserId;
        DepartureCleaningDate = reservationList.DepartureCleaningDate;
        DepartureCarpetUserId = reservationList.DepartureCarpetUserId;
        DepartureCarpetDate = reservationList.DepartureCarpetDate;
        DepartureInspectorUserId = reservationList.DepartureInspectorUserId;
        DepartureInspectingDate = reservationList.DepartureInspectingDate;
    }
}

