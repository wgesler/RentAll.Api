namespace RentAll.Api.Dtos.Accounting.Invoices;

public class CreateInvoiceMonthlyDataDto
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
