using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class UpdateLedgerLineDto
{
	public int LedgerLineId { get; set; }
	public int ChartOfAccountId { get; set; }
	public TransactionType TransactionType { get; set; }
	public Guid? InvoiceId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid(int id)
	{
		if (id == 0)
			return (false, "LedgerLine ID is required");

		if (LedgerLineId != id)
			return (false, "LedgerLine ID mismatch");

		if (ChartOfAccountId == 0)
			return (false, "ChartOfAccountId is required");

		return (true, null);
	}

	public LedgerLine ToModel()
	{
		return new LedgerLine
		{
			LedgerLineId = LedgerLineId,
			ChartOfAccountId = ChartOfAccountId,
			TransactionType = TransactionType,
			InvoiceId = InvoiceId,
			PropertyId = PropertyId,
			ReservationId = ReservationId,
			Amount = Amount
		};
	}
}
