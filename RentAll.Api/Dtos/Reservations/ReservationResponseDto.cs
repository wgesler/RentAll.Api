using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations;

public class ReservationResponseDto
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public int ReservationTypeId { get; set; }
	public int ReservationStatusId { get; set; }
	public string PropertyCode { get; set; }
	public string PropertyAddress {get; set; }
	public int PropertyStatusId { get; set; }
	public string ContactName { get; set; }
	public string ContactPhone { get; set; }
	public string ContactEmail { get; set; }
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


	public ReservationResponseDto(Reservation reservation)
	{
		ReservationId = reservation.ReservationId;
		OrganizationId = reservation.OrganizationId;
		AgentId = reservation.AgentId;
		PropertyId = reservation.PropertyId;
		ContactId = reservation.ContactId;
		ReservationTypeId = (int)reservation.ReservationType;
		ReservationStatusId = (int)reservation.ReservationStatus;
		PropertyCode = reservation.PropertyCode;
		PropertyAddress = reservation.PropertyAddress;
		PropertyStatusId = (int)reservation.PropertyStatus;
		ContactName = reservation.ContactName;
		ContactPhone = reservation.ContactPhone;
		ContactEmail = reservation.ContactEmail;
		TenantName = reservation.TenantName;
		ArrivalDate = reservation.ArrivalDate;
		DepartureDate = reservation.DepartureDate;
		CheckInTimeId = (int)reservation.CheckInTime;
		CheckOutTimeId = (int)reservation.CheckOutTime;
		BillingTypeId = (int)reservation.BillingType;
		BillingRate = reservation.BillingRate;
		NumberOfPeople = reservation.NumberOfPeople;
		Deposit = reservation.Deposit;
		CheckoutFee = reservation.CheckoutFee;
		MaidServiceFee = reservation.MaidServiceFee;
		FrequencyId = reservation.FrequencyId;
		PetFee = reservation.PetFee;
		ExtraFee = reservation.ExtraFee;
		ExtraFeeName = reservation.ExtraFeeName;
		Taxes = reservation.Taxes;
		IsActive = reservation.IsActive;
		CreatedOn = reservation.CreatedOn;
		CreatedBy = reservation.CreatedBy;
		ModifiedOn = reservation.ModifiedOn;
		ModifiedBy = reservation.ModifiedBy;
	}
}


