namespace RentAll.Infrastructure.Entities.Accounting;

public class InvoiceSetActiveByIdResultEntity
{
    public int InvoiceUpdated { get; set; }
    public Guid? InvoiceId { get; set; }
    public int? OfficeId { get; set; }
}
