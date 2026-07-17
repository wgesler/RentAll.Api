namespace RentAll.Api.Dtos.Reports;

public class EscrowReportTotalsResponseDto
{
    public decimal ArBalance { get; set; }
    public decimal Prepaids { get; set; }
    public decimal NotCollected { get; set; }
    public decimal Total { get; set; }
    public decimal E2 { get; set; }

    public EscrowReportTotalsResponseDto(EscrowReportTotals totals)
    {
        ArBalance = totals.ArBalance;
        Prepaids = totals.Prepaids;
        NotCollected = totals.NotCollected;
        Total = totals.Total;
        E2 = totals.E2;
    }
}
