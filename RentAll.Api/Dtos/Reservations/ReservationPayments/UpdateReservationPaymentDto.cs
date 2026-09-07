namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

public class UpdateReservationPaymentDto
{
    public int ReservationPaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReservationPaymentId <= 0)
            return (false, "ReservationPaymentId is required");

        if (ReservationId == Guid.Empty)
            return (false, "ReservationId is required");

        if (Amount < 0)
            return (false, "Amount must be zero or greater");

        if (StartDate > EndDate)
            return (false, "StartDate must be on or before EndDate");

        return (true, null);
    }

    public ReservationPayment ToModel(Guid organizationId, Guid currentUser)
    {
        return new ReservationPayment
        {
            ReservationPaymentId = ReservationPaymentId,
            OrganizationId = organizationId,
            ReservationId = ReservationId,
            Amount = Amount,
            StartDate = StartDate,
            EndDate = EndDate,
            ModifiedBy = currentUser
        };
    }
}
