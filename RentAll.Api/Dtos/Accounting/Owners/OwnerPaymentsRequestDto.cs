namespace RentAll.Api.Dtos.Accounting.Owners;

public class OwnerPaymentsRequestDto
{
    public DateOnly PaymentDate { get; set; }
    public List<OwnerPaymentRequestDto> Payments { get; set; } = new List<OwnerPaymentRequestDto>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (Payments.Count <= 0)
            return (false, "No owner payments submitted");

        foreach (var payment in Payments)
        {
            var (isValid, errorMessage) = payment.IsValid();
            if (!isValid)
                return (false, errorMessage);
        }

        return (true, null);
    }

    public OwnerPayments ToModel()
    {
        return new OwnerPayments
        {
            PaymentDate = PaymentDate,
            Payments = Payments.Select(payment => payment.ToModel()).ToList()
        };
    }
}
