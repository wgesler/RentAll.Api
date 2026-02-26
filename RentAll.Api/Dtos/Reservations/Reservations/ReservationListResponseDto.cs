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
    public string TenantName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }
    public DateTimeOffset ArrivalDate { get; set; }
    public DateTimeOffset DepartureDate { get; set; }
    public int ReservationStatusId { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

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
        TenantName = reservationList.TenantName;
        CompanyName = reservationList.CompanyName;
        AgentCode = reservationList.AgentCode;
        MonthlyRate = reservationList.MonthlyRate;
        ArrivalDate = reservationList.ArrivalDate;
        DepartureDate = reservationList.DepartureDate;
        ReservationStatusId = (int)reservationList.ReservationStatus;
        CurrentInvoiceNo = reservationList.CurrentInvoiceNo;
        CreditDue = reservationList.CreditDue;
        IsActive = reservationList.IsActive;
        CreatedOn = reservationList.CreatedOn;
    }
}

