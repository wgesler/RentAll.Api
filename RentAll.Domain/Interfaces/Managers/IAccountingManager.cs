using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
	List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservationId);
	Task<Reservation> ApplyInvoiceToReservationAsync(Invoice i);
	Task ApplyPaymentToReservationAsync(Guid reservationId, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, Guid currentUser);
}
