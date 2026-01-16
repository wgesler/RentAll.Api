namespace RentAll.Infrastructure.Entities;

public class ReservationEntity
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public string OfficeName { get; set; } = string.Empty;
	public string ReservationCode { get; set; } = string.Empty;
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public string ContactName { get; set; } = string.Empty;
	public int ReservationTypeId { get; set; }
	public int ReservationStatusId { get; set; }
	public int ReservationNoticeId { get; set; }
	public int NumberOfPeople { get; set; }
	public string? TenantName { get; set; }
	public DateTimeOffset ArrivalDate { get; set; }
	public DateTimeOffset DepartureDate { get; set; }
	public int CheckInTimeId { get; set; }
	public int CheckOutTimeId { get; set; }
	public int BillingTypeId { get; set; }
	public decimal BillingRate { get; set; }
	public decimal Deposit { get; set; }
	public int DepositTypeId { get; set; }
	public decimal DepartureFee { get; set; }
	public bool HasPets { get; set; }
	public decimal PetFee { get; set; }
	public int NumberOfPets { get; set; }
	public string? PetDescription { get; set; }
	public bool MaidService { get; set; }
	public decimal MaidServiceFee { get; set; }
	public int FrequencyId { get; set; }
	public decimal Taxes { get; set; }
	public decimal ExtraFee { get; set; }
	public string ExtraFeeName { get; set; } = string.Empty;
	public decimal ExtraFee2 { get; set; }
	public string ExtraFee2Name { get; set; } = string.Empty;
	public string? Notes { get; set; }
	public bool AllowExtensions { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }

}
