using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class LedgerLine
{
	public Guid LedgerLineId { get; set; }
	public Guid InvoiceId { get; set; }
	public Guid ChartOfAccountId { get; set; }
	public TransactionType TransactionType { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;
}
