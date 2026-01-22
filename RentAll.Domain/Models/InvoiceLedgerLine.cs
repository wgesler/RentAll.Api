namespace RentAll.Domain.Models;

public class InvoiceLedgerLine
{
	public Guid InvoiceId { get; set; }
	public int LedgerLineId { get; set; }
}
