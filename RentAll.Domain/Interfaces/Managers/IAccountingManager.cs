using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
	Task<Reservation> ApplyInvoiceToReservationAsync(Invoice i);
	Task ApplyPaymentToReservationAsync(Guid reservationId, Guid organizationId, string offices, int costCodeId, string description, decimal amountPaid, Guid currentUser);
	Task<List<LedgerLine>> CreateLedgerLinesForReservationIdAsync(Reservation reservation, DateTimeOffset startDate, DateTimeOffset endDate);
}
