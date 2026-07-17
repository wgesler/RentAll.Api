namespace RentAll.Domain.Models;

public class EscrowReportTotals
{
    public decimal ArBalance { get; set; }
    public decimal Prepaids { get; set; }
    public decimal NotCollected { get; set; }
    public decimal Total { get; set; }
    public decimal E2 { get; set; }
}
