namespace RentAll.Infrastructure.Entities.Reservations;

public class ReservationDepartureEntity
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
}
