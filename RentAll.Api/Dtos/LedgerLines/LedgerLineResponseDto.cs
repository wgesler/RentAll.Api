using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class LedgerLineResponseDto
{
	public int LedgerLineId { get; set; }
	public Guid InvoiceId { get; set; }
	public Guid? ChartOfAccountId { get; set; }
	public int TransactionTypeId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;

	public LedgerLineResponseDto(LedgerLine ledgerLine)
	{
		LedgerLineId = ledgerLine.LedgerLineId;
		InvoiceId = ledgerLine.InvoiceId;
		ChartOfAccountId = ledgerLine.ChartOfAccountId;
		TransactionTypeId = (int)ledgerLine.TransactionType;
		PropertyId = ledgerLine.PropertyId;
		ReservationId = ledgerLine.ReservationId;
		Amount = ledgerLine.Amount;
		Description = ledgerLine.Description;
	}
}
