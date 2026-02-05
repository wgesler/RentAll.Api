namespace RentAll.Api.Dtos.Invoices;

public class CreateInvoiceMonthlyDataDto
{
	public string InvoiceCode { get; set; } = string.Empty;
	public Guid ReservationId { get; set; }
	public DateTimeOffset StartDate { get; set; }
	public DateTimeOffset EndDate { get; set; }
}
