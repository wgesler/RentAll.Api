namespace RentAll.Api.Dtos.Accounting.Bills;

public class BillPaymentRequestDto
{
    public DateOnly PaymentDate { get; set; }
    public int ChartOfAccountId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int PaymentTypeId { get; set; }
    public List<int> Bills { get; set; } = new List<int>();

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (PaymentDate == default)
            return (false, "PaymentDate is required");

        if (ChartOfAccountId <= 0)
            return (false, "ChartOfAccountId is required");

        if (PaymentTypeId < 0 || PaymentTypeId > 4)
            return (false, "Invalid payment type");

        if (Amount == 0)
            return (false, "No payment submitted");

        if (Bills.Count <= 0)
            return (false, "No bills submitted for payment");

        if (Bills.Any(billId => billId <= 0))
            return (false, "Invalid bill id submitted for payment");

        return (true, null);
    }
}
