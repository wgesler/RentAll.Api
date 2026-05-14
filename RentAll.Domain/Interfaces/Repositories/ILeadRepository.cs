using RentAll.Domain.Models.Leads;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    #region Rental leads
    Task<IEnumerable<LeadRental>> GetRentalsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadRental?> GetRentalByIdAsync(int rentalId);
    Task<LeadRental> CreateRentalAsync(LeadRental rental);
    Task<LeadRental> UpdateRentalByIdAsync(LeadRental rental);
    Task DeleteRentalByIdAsync(int rentalId);
    #endregion

    #region Owner leads
    Task<IEnumerable<LeadOwner>> GetOwnersByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadOwner?> GetOwnerByIdAsync(int ownerId);
    Task<LeadOwner> CreateOwnerAsync(LeadOwner owner);
    Task<LeadOwner> UpdateOwnerByIdAsync(LeadOwner owner);
    Task DeleteOwnerByIdAsync(int ownerId);
    #endregion

    #region General leads
    Task<IEnumerable<LeadGeneral>> GetGeneralsByOfficeIdsAsync(Guid organizationId, string officeIds);
    Task<LeadGeneral?> GetGeneralByIdAsync(int generalId);
    Task<LeadGeneral> CreateGeneralAsync(LeadGeneral lead);
    Task<LeadGeneral> UpdateGeneralByIdAsync(LeadGeneral lead);
    Task DeleteGeneralByIdAsync(int generalId);
    #endregion
}
