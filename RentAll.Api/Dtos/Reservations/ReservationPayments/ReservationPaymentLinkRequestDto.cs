namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>Create a pay-request link for a reservation (payment-link and payment-request-email).</summary>
public class ReservationPaymentLinkRequestDto
{
    public Guid ReservationId { get; set; }

    public ReservationPaymentLinkRequestDto(Guid reservationId)
    {
        ReservationId = reservationId;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");
        return (true, null);
    }
}
