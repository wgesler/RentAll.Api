namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class PaymentBillAllocationDto
{
    public Guid ReceiptId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? CostCodeId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (ReceiptId == Guid.Empty)
            return (false, "ReceiptId is required");

        if (Amount == 0)
            return (false, "Amount is required");

        return (true, null);
    }

    public PaymentBillAllocation ToModel()
    {
        return new PaymentBillAllocation
        {
            ReceiptId = ReceiptId,
            Amount = Amount,
            Description = Description?.Trim() ?? string.Empty,
            CostCodeId = CostCodeId is > 0 ? CostCodeId : null
        };
    }
}
