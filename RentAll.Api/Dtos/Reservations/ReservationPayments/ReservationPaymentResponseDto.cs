namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

public class ReservationPaymentResponseDto
{
    public int ReservationPaymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public string ModifiedByName { get; set; } = string.Empty;

    public ReservationPaymentResponseDto(ReservationPayment payment)
    {
        ReservationPaymentId = payment.ReservationPaymentId;
        OrganizationId = payment.OrganizationId;
        ReservationId = payment.ReservationId;
        Amount = payment.Amount;
        StartDate = payment.StartDate;
        EndDate = payment.EndDate;
        CreatedOn = payment.CreatedOn;
        CreatedBy = payment.CreatedBy;
        CreatedByName = payment.CreatedByName;
        ModifiedOn = payment.ModifiedOn;
        ModifiedBy = payment.ModifiedBy;
        ModifiedByName = payment.ModifiedByName;
    }
}
