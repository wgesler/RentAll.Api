using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations;

public class UpdateReservationDto
{
	public Guid ReservationId { get; set; }
	public Guid OrganizationId { get; set; }
	public Guid? AgentId { get; set; }
	public Guid PropertyId { get; set; }
	public Guid ContactId { get; set; }
	public int ReservationTypeId { get; set; }
	public int ReservationStatusId { get; set; }
	public int ReservationNoticeId { get; set; }
	public int NumberOfPeople { get; set; }
	public bool HasPets { get; set; }
	public string? TenantName { get; set; }
	public DateTimeOffset ArrivalDate { get; set; }
	public DateTimeOffset DepartureDate { get; set; }
	public int CheckInTimeId { get; set; }
	public int CheckOutTimeId { get; set; }
	public int BillingTypeId { get; set; }
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

	public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
	{
		if (id == Guid.Empty)
			return (false, "ReservationId is required");

		if (ReservationId != id)
			return (false, "ReservationId mismatch");

		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (PropertyId == Guid.Empty)
			return (false, "PropertyId is required");

		if (string.IsNullOrWhiteSpace(TenantName))
			return (false, "TenantName is required");

		if (ContactId == Guid.Empty)
			return (false, "ContactId is required");

		if (ArrivalDate >= DepartureDate)
			return (false, "DepartureDate must be after ArrivalDate");

		if (BillingRate < 0)
			return (false, "BillingRate must be zero or greater");

		if (NumberOfPeople < 0)
			return (false, "NumberOfPeople must be zero or greater");

		if (!Enum.IsDefined(typeof(ReservationType), ReservationTypeId))
			return (false, $"Invalid ReservationTypeId value: {ReservationTypeId}");

		if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId))
			return (false, $"Invalid ReservationStatusId value: {ReservationStatusId}");

		if (!Enum.IsDefined(typeof(ReservationNotice), ReservationNoticeId))
			return (false, $"Invalid ReservationNoticeId value: {ReservationNoticeId}");

		if (!Enum.IsDefined(typeof(CheckInTime), CheckInTimeId))
			return (false, $"Invalid CheckInTimeId value: {CheckInTimeId}");

		if (!Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId))
			return (false, $"Invalid CheckOutTimeId value: {CheckOutTimeId}");

		if (!Enum.IsDefined(typeof(BillingType), BillingTypeId))
			return (false, $"Invalid BillingTypeId value: {BillingTypeId}");

		return (true, null);
	}

	public Reservation ToModel( Guid currentUser)
	{
		return new Reservation
		{
			OrganizationId = OrganizationId,
			ReservationId = ReservationId,
			AgentId = AgentId,
			PropertyId = PropertyId,
			ContactId = ContactId,
			ReservationType = (ReservationType)ReservationTypeId,
			ReservationStatus = (ReservationStatus)ReservationStatusId,
			ReservationNotice = (ReservationNotice)ReservationNoticeId,
			NumberOfPeople = NumberOfPeople,
			HasPets = HasPets,
			TenantName = TenantName,
			ArrivalDate = ArrivalDate,
			DepartureDate = DepartureDate,
			CheckInTime = (CheckInTime)CheckInTimeId,
			CheckOutTime = (CheckOutTime)CheckOutTimeId,
			BillingType = (BillingType)BillingTypeId,
			BillingRate = BillingRate,
			Deposit = Deposit,
			DepartureFee = DepartureFee,
			MaidServiceFee = MaidServiceFee,
			FrequencyId = FrequencyId,
			PetFee = PetFee,
			ExtraFee = ExtraFee,
			ExtraFeeName = ExtraFeeName ?? string.Empty,
			Taxes = Taxes,
			Notes = Notes,
			IsActive = IsActive,
			ModifiedBy = currentUser
		};
	}
}


