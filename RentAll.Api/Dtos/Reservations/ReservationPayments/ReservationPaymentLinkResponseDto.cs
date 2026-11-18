namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

public class ReservationPaymentLinkResponseDto
{
    public string PaymentUrl { get; set; } = string.Empty;

    public ReservationPaymentLinkResponseDto(string paymentUrl)
    {
        PaymentUrl = paymentUrl;
    }
}
