namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

public class ApplyReservationRentChangeDto
{
    public Guid ReservationId { get; set; }
    public decimal NewAmount { get; set; }
    public DateOnly EffectiveDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        if (NewAmount < 0)
            return (false, "NewAmount must be zero or greater");

        return (true, null);
    }
}
