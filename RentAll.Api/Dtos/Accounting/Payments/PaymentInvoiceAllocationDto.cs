namespace RentAll.Api.Dtos.Accounting.Payments;

using RentAll.Domain.Models;

public class PaymentInvoiceAllocationDto
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (InvoiceId == Guid.Empty)
            return (false, "InvoiceId is required");

        if (Amount == 0)
            return (false, "Amount is required");

        return (true, null);
    }

    public PaymentInvoiceAllocation ToModel()
    {
        return new PaymentInvoiceAllocation
        {
            InvoiceId = InvoiceId,
            Amount = Amount,
            Description = Description?.Trim() ?? string.Empty
        };
    }
}
