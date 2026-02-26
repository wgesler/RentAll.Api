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
    public string TenantName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string AgentCode { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }
    public DateTimeOffset ArrivalDate { get; set; }
    public DateTimeOffset DepartureDate { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public int CurrentInvoiceNo { get; set; }
    public decimal CreditDue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

