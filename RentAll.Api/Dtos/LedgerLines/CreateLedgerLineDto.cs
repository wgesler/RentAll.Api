using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class CreateLedgerLineDto
{
	public int ChartOfAccountId { get; set; }
	public TransactionType TransactionType { get; set; }
	public Guid? InvoiceId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (ChartOfAccountId == 0)
			return (false, "ChartOfAccountId is required");

		return (true, null);
	}

	public LedgerLine ToModel()
	{
		return new LedgerLine
		{
			ChartOfAccountId = ChartOfAccountId,
			TransactionType = TransactionType,
			InvoiceId = InvoiceId,
			PropertyId = PropertyId,
			ReservationId = ReservationId,
			Amount = Amount
		};
	}
}
