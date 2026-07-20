namespace RentAll.Domain.Models;

public class ActiveInvoiceByAccountingMonthCriteria
{
    public Guid OrganizationId { get; set; }
    public string OfficeIds { get; set; } = string.Empty;
    public DateOnly AccountingPeriod { get; set; }
}
