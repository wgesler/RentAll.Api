using RentAll.Domain.Models.Leads;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    #region Rental leads
    Task<IEnumerable<LeadRental>> GetRentalsAsync();
    Task<LeadRental?> GetRentalByIdAsync(int rentalId);
    Task<LeadRental> CreateRentalAsync(LeadRental rental);
    Task<LeadRental> UpdateRentalByIdAsync(LeadRental rental);
    Task DeleteRentalByIdAsync(int rentalId);
    #endregion

    #region Owner leads
    Task<IEnumerable<LeadOwner>> GetOwnersAsync();
    Task<LeadOwner?> GetOwnerByIdAsync(int ownerId);
    Task<LeadOwner> CreateOwnerAsync(LeadOwner owner);
    Task<LeadOwner> UpdateOwnerByIdAsync(LeadOwner owner);
    Task DeleteOwnerByIdAsync(int ownerId);
    #endregion
}
