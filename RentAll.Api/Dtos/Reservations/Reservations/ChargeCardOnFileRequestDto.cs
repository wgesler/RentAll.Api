namespace RentAll.Api.Dtos.Reservations.Reservations;

/// <summary>Charge the reservation's saved Number card; staff enter the amount.</summary>
public class ChargeCardOnFileRequestDto
{
    /// <summary>From the route; set by the controller.</summary>
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");
        if (Amount <= 0)
            return (false, "Amount must be greater than zero.");
        if (decimal.Round(Amount, 2) != Amount)
            return (false, "Amount can have at most two decimal places.");
        return (true, null);
    }
}
