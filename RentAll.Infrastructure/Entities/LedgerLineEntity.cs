namespace RentAll.Infrastructure.Entities;

public class LedgerLineEntity
{
	public int LedgerLineId { get; set; }
	public int ChartOfAccountId { get; set; }
	public int TransactionTypeId { get; set; }
	public Guid? InvoiceId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }
}
