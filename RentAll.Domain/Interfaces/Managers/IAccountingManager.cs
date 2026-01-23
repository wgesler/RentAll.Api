using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Managers;

public interface IAccountingManager
{
	List<LedgerLine> GetLedgerLinesByReservationIdAsync(Reservation reservationId);
}
