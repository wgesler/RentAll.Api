using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class RecapReportResponseDto
{
    public List<RecapReportRowResponseDto> Rows { get; set; } = [];

    public RecapReportResponseDto(RecapReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new RecapReportRowResponseDto(row)).ToList();
    }
}
