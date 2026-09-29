namespace RentAll.Domain.Models;

public class ReservationActiveStateResult
{
    public bool ReservationUpdated { get; init; }
    public int InvoicesAffected { get; init; }
    public IReadOnlyList<Guid> AffectedInvoiceIds { get; init; } = Array.Empty<Guid>();
}
