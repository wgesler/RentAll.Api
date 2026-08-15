namespace RentAll.Api.Dtos.Reports;

public class RecapReportResponseDto
{
    public List<RecapReportRowResponseDto> Rows { get; set; } = [];
    public string RentalIncomeParentAccountNo { get; set; } = string.Empty;

    public RecapReportResponseDto(RecapReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new RecapReportRowResponseDto(row)).ToList();
        RentalIncomeParentAccountNo = report.RentalIncomeParentAccountNo ?? string.Empty;
    }
}
