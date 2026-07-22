namespace RentAll.Domain.Models;

public class PaymentInvoiceAllocation
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
