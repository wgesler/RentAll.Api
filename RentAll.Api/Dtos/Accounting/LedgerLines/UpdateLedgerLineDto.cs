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
    public DateOnly LedgerLineDate { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (InvoiceId == Guid.Empty)
            return (false, "InvoiceId is required");

        if (CostCodeId <= 0)
            return (false, "CostCodeId is required");
        if (string.IsNullOrWhiteSpace(Description))
            return (false, "Description is required");
        if (LedgerLineDate == default)
            return (false, "LedgerLineDate is required");

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
            Description = Description.Trim(),
            LedgerLineDate = LedgerLineDate,
            ModifiedBy = currentUser
        };
    }
}
