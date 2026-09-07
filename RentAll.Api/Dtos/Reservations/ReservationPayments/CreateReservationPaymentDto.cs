namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

public class CreateReservationPaymentDto
{
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            OrganizationId = organizationId,
            ReservationId = ReservationId,
            Amount = Amount,
            StartDate = StartDate,
            EndDate = EndDate,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
    }
}
