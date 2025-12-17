using RentAll.Domain.Enums;

namespace RentAll.Domain.Models.Reservations;

public class Reservation
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public string TenantName { get; set; } = string.Empty;
	public Guid ClientId { get; set; }
	public ClientType ClientType { get; set; }
	public ReservationStatus ReservationStatus { get; set; }
	public DateTimeOffset ArrivalDate { get; set; }
	public DateTimeOffset DepartureDate { get; set; }
	public CheckInTime CheckInTime { get; set; }
	public CheckOutTime CheckOutTime { get; set; }
	public BillingType BillingType { get; set; }
	public decimal BillingRate { get; set; }
	public int NumberOfPeople { get; set; }
	public decimal? Deposit { get; set; }
	public decimal CheckoutFee { get; set; }
	public decimal MaidServiceFee { get; set; }
	public int FrequencyId { get; set; }
	public decimal PetFee { get; set; }
	public decimal ExtraFee { get; set; }
	public string ExtraFeeName { get; set; } = string.Empty;
	public decimal Taxes { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
	public bool IsActive { get; set; }
}


