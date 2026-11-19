namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ChargeCardOnFileRequestDto
{
    public decimal Amount { get; set; }
}

public class ChargeCardOnFileResponseDto
{
    public bool Approved { get; set; }
    public long TransactionId { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public bool IsPartialApproval { get; set; }
    public Guid? PaymentId { get; set; }
    public string? PaymentCode { get; set; }
    public string? Message { get; set; }
}
