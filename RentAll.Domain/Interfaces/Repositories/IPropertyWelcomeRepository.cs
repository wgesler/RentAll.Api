using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyWelcomeRepository
{
	// Creates
	Task<PropertyWelcome> CreateAsync(PropertyWelcome propertyWelcome);

	// Selects
	Task<PropertyWelcome?> GetByPropertyIdAsync(Guid propertyId, Guid organizationId);

	// Updates
	Task<PropertyWelcome> UpdateByIdAsync(PropertyWelcome propertyWelcome);

	// Deletes
	Task DeleteByPropertyIdAsync(Guid propertyId);
}


