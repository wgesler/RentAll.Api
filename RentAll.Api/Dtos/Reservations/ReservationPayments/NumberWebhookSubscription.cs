namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>The subscription inside an Number PFS webhook (SUBSCRIP_CREATE); its Id is the consent id.</summary>
public class NumberWebhookSubscription
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string? Result { get; set; }
    public string? Status { get; set; }
}
