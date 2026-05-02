namespace RentAll.Api.Dtos.Accounting.LedgerLines;

public class LedgerLineResponseDto
{
    public Guid LedgerLineId { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid? ReservationId { get; set; }
    public int CostCodeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly LedgerLineDate { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public LedgerLineResponseDto(LedgerLine ledgerLine)
    {
        LedgerLineId = ledgerLine.LedgerLineId;
        InvoiceId = ledgerLine.InvoiceId;
        LineNumber = ledgerLine.LineNumber;
        ReservationId = ledgerLine.ReservationId;
        CostCodeId = ledgerLine.CostCodeId;
        Amount = ledgerLine.Amount;
        Description = ledgerLine.Description;
        LedgerLineDate = ledgerLine.LedgerLineDate;
        CreatedOn = ledgerLine.CreatedOn;
        CreatedBy = ledgerLine.CreatedBy;
        ModifiedOn = ledgerLine.ModifiedOn;
        ModifiedBy = ledgerLine.ModifiedBy;
    }
}
