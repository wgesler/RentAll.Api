using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Rentals;

public class Reservation
{
    public Guid ReservationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PropertyId { get; set; }
	public string? PropertyCode { get; set; }
	public string? PropertyAddress { get; set; }
	public PropertyStatus PropertyStatus { get; set; }
	public Guid ContactId { get; set; }
    public ClientType ClientType { get; set; }
    public ReservationStatus ReservationStatus { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ArrivalDate { get; set; }
    public DateTimeOffset? DepartureDate { get; set; }
    public CheckInTime CheckInTime { get; set; }
    public CheckOutTime CheckOutTime { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
	public int Bedrooms { get; set; }
	public decimal Bathrooms { get; set; }
	public int NumberOfPeople { get; set; }
    public decimal Deposit { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal Taxes { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}


