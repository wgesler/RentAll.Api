using RentAll.Domain.Models.Reservations;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
	// Creates
	Task<Reservation> CreateAsync(Reservation reservation);

	// Selects
	Task<IEnumerable<Reservation>> GetAllAsync(Guid organizationId);
	Task<Reservation?> GetByIdAsync(Guid reservationId, Guid organizationId);
	Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);
	Task<IEnumerable<Reservation>> GetByClientIdAsync(Guid clientId, Guid organizationId);
	Task<IEnumerable<Reservation>> GetActiveReservationsAsync(Guid organizationId);

	// Updates
	Task<Reservation> UpdateByIdAsync(Reservation reservation);

	// Deletes
	Task DeleteByIdAsync(Guid reservationId);
}


