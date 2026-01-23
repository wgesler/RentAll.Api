using RentAll.Api.Dtos.LedgerLines;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class InvoiceMonthlyDataResponseDto
{
	public string Invoice { get; set; } = string.Empty;
	public Guid ReservationId { get; set; }
	public List<LedgerLineResponseDto> LedgerLines { get; set; } = new List<LedgerLineResponseDto>();

	public InvoiceMonthlyDataResponseDto(InvoiceMonthlyData i)
	{
		Invoice = i.Invoice;
		ReservationId = i.ReservationId;
		LedgerLines = i.LedgerLines.Select(l => new LedgerLineResponseDto(l)).ToList();
	}
}
