namespace RentAll.Api.Dtos.Accounting.Payments;

public class PaymentLedgerLineResponseDto
{
    public Guid LedgerLineId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public Guid? ReservationId { get; set; }
    public int CostCodeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly LedgerLineDate { get; set; }
    public Guid PaymentId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public PaymentLedgerLineResponseDto(PaymentLedgerLine line)
    {
        LedgerLineId = line.LedgerLineId;
        InvoiceId = line.InvoiceId;
        InvoiceCode = line.InvoiceCode;
        LineNumber = line.LineNumber;
        ReservationId = line.ReservationId;
        CostCodeId = line.CostCodeId;
        Amount = line.Amount;
        Description = line.Description;
        LedgerLineDate = line.LedgerLineDate;
        PaymentId = line.PaymentId;
        CreatedOn = line.CreatedOn;
        CreatedBy = line.CreatedBy;
        ModifiedOn = line.ModifiedOn;
        ModifiedBy = line.ModifiedBy;
    }
}
