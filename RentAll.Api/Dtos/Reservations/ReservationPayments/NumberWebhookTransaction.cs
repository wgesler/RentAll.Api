namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>The transaction inside an Number PFS webhook. Amount is in cents.</summary>
public class NumberWebhookTransaction
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string? Result { get; set; }
    public string? TxnCode { get; set; }
    public int ErrCode { get; set; }
    public string? ErrMsg { get; set; }
}
