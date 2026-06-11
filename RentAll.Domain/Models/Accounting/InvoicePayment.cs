namespace RentAll.Domain.Models;

public class InvoicePayment
{
    public List<Invoice> Invoices { get; set; } = new List<Invoice>();
    public List<InvoicePaymentApplication> PaymentApplications { get; set; } = new List<InvoicePaymentApplication>();
}

public class InvoicePaymentApplication
{
    public Invoice Invoice { get; set; } = null!;
    public LedgerLine PaymentLedgerLine { get; set; } = null!;
}
