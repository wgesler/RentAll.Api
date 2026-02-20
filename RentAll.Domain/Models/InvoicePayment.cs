namespace RentAll.Domain.Models;

public class InvoicePayment
{
    public List<Invoice> Invoices { get; set; } = new List<Invoice>();
    public decimal CreditRemaining { get; set; }
}
