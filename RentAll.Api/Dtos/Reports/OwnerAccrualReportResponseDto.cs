using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class OwnerAccrualReportResponseDto
{
    public List<OwnerAccrualReportRowResponseDto> Rows { get; set; } = [];
    public List<OwnerCashReportPropertyActivityLineResponseDto> PropertyActivityLines { get; set; } = [];

    public OwnerAccrualReportResponseDto(OwnerAccrualReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new OwnerAccrualReportRowResponseDto(row)).ToList();
        PropertyActivityLines = (report.PropertyActivityLines ?? [])
            .Select(line => new OwnerCashReportPropertyActivityLineResponseDto(line))
            .ToList();
    }
}
