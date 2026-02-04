using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Invoices;

public class InvoicePaymentResponseDto
{
	public List<InvoiceResponseDto> Invoices { get; set; } = new List<InvoiceResponseDto>();
	public decimal CreditRemaining { get; set; }

	public InvoicePaymentResponseDto(InvoicePayment i)
	{
		Invoices = i.Invoices.Select(invoice => new InvoiceResponseDto(invoice)).ToList();
		CreditRemaining = i.CreditRemaining;
	}
}