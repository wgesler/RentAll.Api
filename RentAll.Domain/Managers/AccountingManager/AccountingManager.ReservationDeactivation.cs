using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public const string UnpaidInvoicesBlockReservationDeactivationMessage =
        "Reservations with unpaid invoices may not be made inactive.";

    public async Task ValidateReservationDeactivationAllowedAsync(Guid organizationId, Guid reservationId)
    {
        if (reservationId == Guid.Empty)
            throw new ArgumentException("ReservationId is required.", nameof(reservationId));

        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, organizationId)
            ?? throw new InvalidOperationException("Reservation not found.");

        if (reservation.OfficeId <= 0)
            throw new InvalidOperationException("Reservation office is required.");

        var unpaidInvoices = (await _accountingRepository.GetInvoicesAsync(new InvoiceGetCriteria
        {
            OrganizationId = organizationId,
            OfficeIds = reservation.OfficeId.ToString(),
            ReservationId = reservationId,
            IncludeInactive = true,
            IncludePaid = false
        })).ToList();

        if (unpaidInvoices.Count > 0)
            throw new InvalidOperationException(UnpaidInvoicesBlockReservationDeactivationMessage);
    }
}
