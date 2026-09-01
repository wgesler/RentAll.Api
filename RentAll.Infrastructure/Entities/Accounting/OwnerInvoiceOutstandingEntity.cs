namespace RentAll.Infrastructure.Entities.Accounting;

public class OwnerInvoiceOutstandingEntity
{
    public Guid OwnerInvoiceOutstandingId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public DateTime AccountingPeriod { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Outstanding { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}
