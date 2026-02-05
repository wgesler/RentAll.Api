using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class LedgerLineResponseDto
{
	public Guid LedgerLineId { get; set; }
	public Guid InvoiceId { get; set; }
	public int LineNumber { get; set; }
	public Guid? ReservationId { get; set; }
	public int CostCodeId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;

	public LedgerLineResponseDto(LedgerLine ledgerLine)
	{
		LedgerLineId = ledgerLine.LedgerLineId;
		InvoiceId = ledgerLine.InvoiceId;
		LineNumber = ledgerLine.LineNumber;
		ReservationId = ledgerLine.ReservationId;
		CostCodeId = ledgerLine.CostCodeId;
		Amount = ledgerLine.Amount;
		Description = ledgerLine.Description;
	}
}
