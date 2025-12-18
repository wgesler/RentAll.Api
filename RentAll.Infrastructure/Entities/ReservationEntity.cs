namespace RentAll.Infrastructure.Entities;

public class ReservationEntity
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public int ReservationTypeId { get; set; }
	public int ReservationStatusId { get; set; }
	public string PropertyCode { get; set; } = string.Empty;
	public string PropertyAddress { get; set; } = string.Empty;
	public int PropertyStatusId { get; set; }
	public string ContactName { get; set; } = string.Empty;
	public string ContactPhone { get; set; } = string.Empty;
	public string ContactEmail { get; set; } = string.Empty;
	public string? TenantName { get; set; }
	public DateTimeOffset ArrivalDate { get; set; }
	public DateTimeOffset DepartureDate { get; set; }
	public int CheckInTimeId { get; set; }
	public int CheckOutTimeId { get; set; }
	public int BillingTypeId { get; set; }
	public decimal BillingRate { get; set; }
	public int NumberOfPeople { get; set; }
	public decimal Deposit { get; set; }
	public decimal CheckoutFee { get; set; }
	public decimal MaidServiceFee { get; set; }
	public int FrequencyId { get; set; }
	public decimal PetFee { get; set; }
	public decimal ExtraFee { get; set; }
	public string ExtraFeeName { get; set; } = string.Empty;
	public decimal Taxes { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

}