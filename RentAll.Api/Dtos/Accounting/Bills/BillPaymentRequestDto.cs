namespace RentAll.Api.Dtos.Accounting.Bills;

public class BillPaymentRequestDto
{
    public DateOnly PaymentDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public List<Guid> Bills { get; set; } = new List<Guid>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (!Enum.IsDefined(typeof(PaymentType), PaymentTypeId))
            return (false, $"Invalid PaymentType value: {PaymentTypeId}");

        if (Amount == 0)
            return (false, "No payment submitted");

        if (Bills.Count <= 0)
            return (false, "No bills submitted for payment");

        if (Bills.Any(billId => billId == Guid.Empty))
            return (false, "Invalid bill id submitted for payment");

        return (true, null);
    }
}
