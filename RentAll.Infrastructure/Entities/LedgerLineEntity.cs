namespace RentAll.Infrastructure.Entities;

public class LedgerLineEntity
{
	public Guid LedgerLineId { get; set; }
	public Guid InvoiceId { get; set; }
	public Guid? ReservationId { get; set; }
	public int CostCodeId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;
}
