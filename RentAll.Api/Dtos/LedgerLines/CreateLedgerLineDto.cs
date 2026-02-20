using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LedgerLines;

public class CreateLedgerLineDto
{
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid? ReservationId { get; set; }
    public int CostCodeId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
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
            InvoiceId = InvoiceId,
            LineNumber = LineNumber,
            ReservationId = ReservationId,
            CostCodeId = CostCodeId,
            Amount = Amount,
            Description = Description,
            CreatedBy = currentUser
        };
    }
}
