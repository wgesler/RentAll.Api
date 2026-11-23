namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>A fresh Number form for the pay-request page to show in its iFrame.</summary>
public class PayRequestFormResponseDto
{
    public string FormUrl { get; set; } = string.Empty;

    public PayRequestFormResponseDto(string formUrl)
    {
        FormUrl = formUrl;
    }
}
