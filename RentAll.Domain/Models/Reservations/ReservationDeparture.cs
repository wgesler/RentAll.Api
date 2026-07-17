using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class ReservationDeparture
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string? AgentCode { get; set; }
    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal BillingRate { get; set; }
    public int BillingTypeId { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public ReservationType ReservationType { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public bool HasPets { get; set; }
    public DepositType DepositType { get; set; }
    public decimal Deposit { get; set; }
    public bool DepositReturned { get; set; }
    public DateOnly SecurityDepositReturnDate { get; set; }
}
