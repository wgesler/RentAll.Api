namespace RentAll.Api.Dtos.Accounting.LedgerLines;

public class UpdateLedgerLineDto
{
    public Guid LedgerLineId { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid? ReservationId { get; set; }
    public int CostCodeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (InvoiceId == Guid.Empty)
            return (false, "InvoiceId is required");

        if (CostCodeId <= 0)
            return (false, "CostCodeId is required");

        if (Amount == 0)
            return (false, "Amount cannot be zero");

        return (true, null);
    }

    public LedgerLine ToModel(Guid currentUser)
    {
        return new LedgerLine
        {
            LedgerLineId = LedgerLineId,
            InvoiceId = InvoiceId,
            LineNumber = LineNumber,
            ReservationId = ReservationId,
            CostCodeId = CostCodeId,
            Amount = Amount,
            Description = Description,
            ModifiedBy = currentUser
        };
    }
}
