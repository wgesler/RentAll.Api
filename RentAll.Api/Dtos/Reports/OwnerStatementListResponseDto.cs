namespace RentAll.Api.Dtos.Reports;

using RentAll.Domain.Models;

public class OwnerStatementListResponseDto
{
    public List<OwnerCashReportRowResponseDto> Rows { get; set; } = [];
    public List<OwnerInvoiceOutstandingResponseDto> OutstandingInvoices { get; set; } = [];

    public OwnerStatementListResponseDto(OwnerStatementListReport report)
    {
        Rows = (report.Rows ?? []).Select(row => new OwnerCashReportRowResponseDto(row)).ToList();
        OutstandingInvoices = (report.OutstandingInvoices ?? [])
            .Select(row => new OwnerInvoiceOutstandingResponseDto(row))
            .ToList();
    }
}
