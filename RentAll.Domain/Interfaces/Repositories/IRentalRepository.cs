using RentAll.Domain.Models.Rentals;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IReservationRepository
{
    // Creates
    Task<Reservation> CreateAsync(Reservation reservation);

	// Selects
	Task<IEnumerable<Reservation>> GetAllAsync();
	Task<IEnumerable<Reservation>> GetActiveReservationsAsync();
	Task<Reservation?> GetByIdAsync(Guid reservationId);
    Task<IEnumerable<Reservation>> GetByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<Reservation>> GetByContactIdAsync(Guid contactId);

    // Updates
    Task<Reservation> UpdateByIdAsync(Reservation reservation);

    // Deletes
    Task DeleteByIdAsync(Guid reservationId);
}


