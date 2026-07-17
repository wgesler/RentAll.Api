using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationDepartureResponseDto
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
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool HasPets { get; set; }
    public int DepositTypeId { get; set; }
    public decimal Deposit { get; set; }
    public bool DepositReturned { get; set; }
    public DateOnly SecurityDepositReturnDate { get; set; }

    public ReservationDepartureResponseDto(ReservationDeparture departure)
    {
        ReservationId = departure.ReservationId;
        ReservationCode = departure.ReservationCode;
        PropertyId = departure.PropertyId;
        PropertyCode = departure.PropertyCode;
        OfficeId = departure.OfficeId;
        OfficeName = departure.OfficeName;
        AgentCode = departure.AgentCode;
        ContactId = departure.ContactId;
        ContactName = departure.ContactName;
        CompanyId = departure.CompanyId;
        CompanyName = departure.CompanyName;
        TenantName = departure.TenantName;
        MonthlyRate = departure.MonthlyRate;
        DailyRate = departure.DailyRate;
        BillingRate = departure.BillingRate;
        BillingTypeId = departure.BillingTypeId;
        ArrivalDate = departure.ArrivalDate;
        DepartureDate = departure.DepartureDate;
        ReservationTypeId = (int)departure.ReservationType;
        ReservationStatusId = (int)departure.ReservationStatus;
        HasPets = departure.HasPets;
        DepositTypeId = (int)departure.DepositType;
        Deposit = departure.Deposit;
        DepositReturned = departure.DepositReturned;
        SecurityDepositReturnDate = departure.SecurityDepositReturnDate;
    }
}
