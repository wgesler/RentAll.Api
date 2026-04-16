namespace RentAll.Api.Dtos.Accounting.Invoices;

public class CreateBillingMonthlyDataDto
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
