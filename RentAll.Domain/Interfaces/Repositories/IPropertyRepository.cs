using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    // Creates
    Task<Property> CreateAsync(Property property);

	// Selects
	Task<IEnumerable<Property>> GetAllAsync(Guid organizationId);
	Task<Property?> GetByIdAsync(Guid propertyId, Guid organizationId);
    Task<Property?> GetByPropertyCodeAsync(string propertyCode, Guid organizationId);
    Task<IEnumerable<Property>> GetByStateAsync(string state, Guid organizationId);
	Task<IEnumerable<Property>> GetBySelectionCriteriaAsync(Guid userId, Guid organizationId);
    Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId);

    // Updates
    Task<Property> UpdateByIdAsync(Property property);

    // Deletes
    Task DeleteByIdAsync(Guid propertyId);
}





