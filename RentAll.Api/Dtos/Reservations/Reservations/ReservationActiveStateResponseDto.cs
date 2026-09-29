using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reservations.Reservations;

public class ReservationActiveStateResponseDto
{
    public int InvoicesAffected { get; set; }
    public IReadOnlyList<Guid> InvoiceIds { get; set; } = Array.Empty<Guid>();

    public ReservationActiveStateResponseDto() { }

    public ReservationActiveStateResponseDto(ReservationActiveStateResult result)
    {
        InvoicesAffected = result.InvoicesAffected;
        InvoiceIds = result.AffectedInvoiceIds;
    }
}
