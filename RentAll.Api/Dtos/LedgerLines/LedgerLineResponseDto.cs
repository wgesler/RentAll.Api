using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class LedgerLineResponseDto
{
	public int LedgerLineId { get; set; }
	public int ChartOfAccountId { get; set; }
	public TransactionType TransactionType { get; set; }
	public Guid? InvoiceId { get; set; }
	public Guid? PropertyId { get; set; }
	public Guid? ReservationId { get; set; }
	public decimal Amount { get; set; }

	public LedgerLineResponseDto(LedgerLine ledgerLine)
	{
		LedgerLineId = ledgerLine.LedgerLineId;
		ChartOfAccountId = ledgerLine.ChartOfAccountId;
		TransactionType = ledgerLine.TransactionType;
		InvoiceId = ledgerLine.InvoiceId;
		PropertyId = ledgerLine.PropertyId;
		ReservationId = ledgerLine.ReservationId;
		Amount = ledgerLine.Amount;
	}
}
