using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyRepository
{
    // Creates
    Task<Property> CreateAsync(Property property);

	// Selects
	Task<IEnumerable<PropertyList>> GetListByOfficeIdAsync(Guid organizationId, string officeAccess);
	Task<IEnumerable<PropertyList>> GetListBySelectionCriteriaAsync(Guid userId, Guid organizationId, string officeAccess);

	Task<Property?> GetByIdAsync(Guid propertyId, Guid organizationId);
    Task<Property?> GetByPropertyCodeAsync(string propertyCode, Guid organizationId);
    Task<bool> ExistsByPropertyCodeAsync(string propertyCode, Guid organizationId);

    // Updates
    Task<Property> UpdateByIdAsync(Property property);

    // Deletes
    Task DeleteByIdAsync(Guid propertyId);
}





