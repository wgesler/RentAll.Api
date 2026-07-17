namespace RentAll.Domain.Models;

public class EscrowReport
{
    public string ReportTitle { get; set; } = "Escrow Report";
    public string PeriodLabel { get; set; } = string.Empty;
    public string? EntityLineLabel { get; set; }
    public List<EscrowReportRow> Rows { get; set; } = [];
    public EscrowReportTotals Totals { get; set; } = new();
    public decimal Cushion { get; set; }
    public decimal EscrowBankBalance { get; set; }
    public string EscrowBankAccountLabel { get; set; } = "Escrow Bank Balance";
    public decimal Transfer { get; set; }
}
