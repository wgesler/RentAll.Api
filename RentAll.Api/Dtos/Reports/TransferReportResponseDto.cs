using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class TransferReportResponseDto
{
    public List<TransferReportRowResponseDto> Rows { get; set; } = [];

    public TransferReportResponseDto(TransferReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new TransferReportRowResponseDto(row)).ToList();
    }
}
