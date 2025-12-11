using RentAll.Domain.Enums;
using RentAll.Domain.Models.Rentals;

namespace RentAll.Api.Dtos.Rentals;

public class UpdateReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid AgentId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContactId { get; set; }
    public int ClientTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool IsActive { get; set; }

    // Details
    public DateTimeOffset? ArrivalDate { get; set; }
    public DateTimeOffset? DepartureDate { get; set; }
    public int CheckInTimeId { get; set; }
    public int CheckOutTimeId { get; set; }
    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public int NumberOfPeople { get; set; }
    public decimal Deposit { get; set; }
    public decimal DepartureFee { get; set; }
    public decimal Taxes { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid id)
    {
        if (id == Guid.Empty)
            return (false, "reservationId is required");

        if (ReservationId != id)
            return (false, "reservationId mismatch");

		if (PropertyId == Guid.Empty)
			return (false, "propertyId is required");

		if (ContactId == Guid.Empty)
			return (false, "contactId is required");

		if (ArrivalDate.HasValue && DepartureDate.HasValue && ArrivalDate >= DepartureDate)
			return (false, "departureDate must be after arrivalDate");

		if (DailyRate < 1)
			return (false, "dailyRate must be zero or greater");

		if (MonthlyRate < 1)
			return (false, "monthlyRate must be zero or greater");

		if (NumberOfPeople < 1)
			return (false, "numberOfPeople must be zero or greater");

		if (!Enum.IsDefined(typeof(ClientType), ClientTypeId))
			return (false, $"Invalid clientTypeId value: {ClientTypeId}");

		if (!Enum.IsDefined(typeof(ReservationStatus), ReservationStatusId))
			return (false, $"Invalid reservationStatusId value: {ReservationStatusId}");

		if (!Enum.IsDefined(typeof(CheckInTime), CheckInTimeId))
			return (false, $"Invalid checkInTimeId value: {CheckInTimeId}");

		if (!Enum.IsDefined(typeof(CheckOutTime), CheckOutTimeId))
			return (false, $"Invalid checkOutTimeId value: {CheckOutTimeId}");

		return (true, null);
    }

    public Reservation ToModel(Reservation existingReservation, Guid currentUser)
    {
        return new Reservation
        {
            ReservationId = ReservationId,
            AgentId = AgentId,
            PropertyId = PropertyId,
            ContactId = ContactId,
            ClientType = (ClientType)ClientTypeId,
            ReservationStatus = (ReservationStatus)ReservationStatusId,
            IsActive = IsActive,
            ArrivalDate = ArrivalDate,
            DepartureDate = DepartureDate,
            CheckInTime = (CheckInTime)CheckInTimeId,
            CheckOutTime = (CheckOutTime)CheckOutTimeId,
            MonthlyRate = MonthlyRate,
            DailyRate = DailyRate,
            NumberOfPeople = NumberOfPeople,
            Deposit = Deposit,
            DepartureFee = DepartureFee,
            Taxes = Taxes,
            CreatedOn = existingReservation.CreatedOn,
            CreatedBy = existingReservation.CreatedBy,
            ModifiedBy = currentUser
        };
    }
}


