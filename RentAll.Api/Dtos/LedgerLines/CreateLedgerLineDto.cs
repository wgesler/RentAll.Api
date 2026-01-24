using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class CreateLedgerLineDto
{
	public Guid InvoiceId { get; set; }
	public Guid ChartOfAccountId { get; set; }
	public int TransactionTypeId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (ChartOfAccountId == Guid.Empty)
			return (false, "ChartOfAccountId is required");

		if (!Enum.IsDefined(typeof(TransactionType), TransactionTypeId))
			return (false, "Invalid TransactionTypeId");

		if (Amount == 0)
			return (false, "Amount cannot be zero");

		return (true, null);
	}

	public LedgerLine ToModel()
	{
		return new LedgerLine
		{
			ChartOfAccountId = ChartOfAccountId,
			TransactionType = (TransactionType)TransactionTypeId,
			InvoiceId = InvoiceId,
			PropertyId = PropertyId,
			ReservationId = ReservationId,
			Amount = Amount,
			Description = Description
		};
	}
}
