using RentAll.Domain.Models.Rentals;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IRentalRepository
{
    // Creates
    Task<Rental> CreateAsync(Rental rental);

    // Selects
    Task<Rental?> GetByIdAsync(Guid rentalId);
    Task<IEnumerable<Rental>> GetActiveRentalsAsync();
    Task<IEnumerable<Rental>> GetByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<Rental>> GetByContactIdAsync(Guid contactId);

    // Updates
    Task<Rental> UpdateByIdAsync(Rental rental);

    // Deletes
    Task DeleteByIdAsync(Guid rentalId);
}

