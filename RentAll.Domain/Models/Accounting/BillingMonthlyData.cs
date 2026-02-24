namespace RentAll.Domain.Models;

public class BillingMonthlyData
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<LedgerLine> LedgerLines { get; set; } = new List<LedgerLine>();
}
