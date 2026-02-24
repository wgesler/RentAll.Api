using RentAll.Api.Dtos.Accounting.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.Invoices;

public class BillingMonthlyDataResponseDto
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public List<LedgerLineResponseDto> LedgerLines { get; set; } = new List<LedgerLineResponseDto>();

    public BillingMonthlyDataResponseDto(BillingMonthlyData i)
    {
        InvoiceCode = i.InvoiceCode;
        OrganizationId = i.OrganizationId;
        LedgerLines = i.LedgerLines.Select(l => new LedgerLineResponseDto(l)).ToList();
    }
}
