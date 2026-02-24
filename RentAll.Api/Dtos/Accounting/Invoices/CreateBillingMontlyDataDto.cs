namespace RentAll.Api.Dtos.Accounting.Invoices;

public class CreateBillingMonthlyDataDto
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}
