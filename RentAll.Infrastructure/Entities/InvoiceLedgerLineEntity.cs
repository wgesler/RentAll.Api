namespace RentAll.Infrastructure.Entities;

public class InvoiceLedgerLineEntity
{
	public Guid InvoiceId { get; set; }
	public int LedgerLineId { get; set; }
}
