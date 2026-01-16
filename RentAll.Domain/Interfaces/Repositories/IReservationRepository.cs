using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
	// Creates
	Task<Reservation> CreateAsync(Reservation reservation);

	// Selects
	Task<IEnumerable<ReservationList>> GetListByOfficeIdAsync(Guid organizationId, string officeAccess);
	Task<Reservation?> GetByIdAsync(Guid reservationId, Guid organizationId);
	Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);

	// Updates
	Task<Reservation> UpdateByIdAsync(Reservation reservation);

	// Deletes
	Task DeleteByIdAsync(Guid reservationId);
}


