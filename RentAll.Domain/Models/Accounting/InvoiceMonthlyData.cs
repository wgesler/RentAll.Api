namespace RentAll.Domain.Models;

public class InvoiceMonthlyData
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public List<LedgerLine> LedgerLines { get; set; } = new List<LedgerLine>();
}
