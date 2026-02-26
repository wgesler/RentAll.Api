using RentAll.Api.Dtos.Accounting.LedgerLines;

namespace RentAll.Api.Dtos.Accounting.Invoices;

public class InvoiceMonthlyDataResponseDto
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public List<LedgerLineResponseDto> LedgerLines { get; set; } = new List<LedgerLineResponseDto>();

    public InvoiceMonthlyDataResponseDto(InvoiceMonthlyData i)
    {
        InvoiceCode = i.InvoiceCode;
        ReservationId = i.ReservationId;
        LedgerLines = i.LedgerLines.Select(l => new LedgerLineResponseDto(l)).ToList();
    }
}
