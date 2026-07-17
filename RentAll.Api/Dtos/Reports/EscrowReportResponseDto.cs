namespace RentAll.Api.Dtos.Reports;

public class EscrowReportResponseDto
{
    public string ReportTitle { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string? EntityLineLabel { get; set; }
    public List<EscrowReportRowResponseDto> Rows { get; set; } = [];
    public EscrowReportTotalsResponseDto Totals { get; set; } = new(new EscrowReportTotals());
    public decimal Cushion { get; set; }
    public decimal EscrowBankBalance { get; set; }
    public string EscrowBankAccountLabel { get; set; } = string.Empty;
    public decimal Transfer { get; set; }

    public EscrowReportResponseDto(EscrowReport report)
    {
        ReportTitle = report.ReportTitle;
        PeriodLabel = report.PeriodLabel;
        EntityLineLabel = report.EntityLineLabel;
        Rows = (report.Rows ?? []).Select(row => new EscrowReportRowResponseDto(row)).ToList();
        Totals = new EscrowReportTotalsResponseDto(report.Totals ?? new EscrowReportTotals());
        Cushion = report.Cushion;
        EscrowBankBalance = report.EscrowBankBalance;
        EscrowBankAccountLabel = report.EscrowBankAccountLabel;
        Transfer = report.Transfer;
    }
}
