namespace RentAll.Api.Dtos.Reports;

public class OwnerCashReportResponseDto
{
    public List<OwnerCashReportRowResponseDto> Rows { get; set; } = [];
    public List<OwnerCashReportPropertyActivityLineResponseDto> PropertyActivityLines { get; set; } = [];

    public OwnerCashReportResponseDto(OwnerCashReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new OwnerCashReportRowResponseDto(row)).ToList();
        PropertyActivityLines = (report.PropertyActivityLines ?? [])
            .Select(line => new OwnerCashReportPropertyActivityLineResponseDto(line))
            .ToList();
    }
}
