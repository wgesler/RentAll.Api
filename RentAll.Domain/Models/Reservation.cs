using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Reservation
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public ReservationType ReservationType { get; set; }
	public ReservationStatus ReservationStatus { get; set; }
	public ReservationNotice ReservationNotice { get; set; }
	public int NumberOfPeople { get; set; }
	public bool HasPets { get; set; }
	public string? TenantName { get; set; }
	public string PropertyCode { get; set; } = string.Empty;
	public string PropertyAddress { get; set; } = string.Empty;
	public PropertyStatus PropertyStatus { get; set; }
	public string ContactName { get; set; } = string.Empty;
	public string ContactPhone { get; set; } = string.Empty;
	public string ContactEmail { get; set; } = string.Empty;
	public DateTimeOffset ArrivalDate { get; set; }
	public DateTimeOffset DepartureDate { get; set; }
	public CheckInTime CheckInTime { get; set; }
	public CheckOutTime CheckOutTime { get; set; }
	public BillingType BillingType { get; set; }
	public decimal BillingRate { get; set; }
	public decimal Deposit { get; set; }
	public decimal DepartureFee { get; set; }
	public decimal MaidServiceFee { get; set; }
	public int FrequencyId { get; set; }
	public decimal PetFee { get; set; }
	public decimal ExtraFee { get; set; }
	public string ExtraFeeName { get; set; } = string.Empty;
	public decimal Taxes { get; set; }
	public string? Notes { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

}


