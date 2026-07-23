namespace RentAll.Api.Dtos.Reports;

using RentAll.Domain.Models;

public class OwnerReportsResponseDto
{
    public OwnerCashReportResponseDto Cash { get; set; }
    public OwnerAccrualReportResponseDto Accrual { get; set; }
    public RecapReportResponseDto Recap { get; set; }

    public OwnerReportsResponseDto(OwnerReportsBundle bundle)
    {
        Cash = new OwnerCashReportResponseDto(bundle.Cash);
        Accrual = new OwnerAccrualReportResponseDto(bundle.Accrual);
        Recap = new RecapReportResponseDto(bundle.Recap);
    }
}
