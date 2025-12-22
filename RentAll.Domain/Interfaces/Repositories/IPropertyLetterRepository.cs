using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IPropertyLetterRepository
{
	// Creates
	Task<PropertyLetter> CreateAsync(PropertyLetter propertyLetter);

	// Selects
	Task<IEnumerable<PropertyLetter>> GetAllAsync();
	Task<PropertyLetter?> GetByPropertyIdAsync(Guid propertyId);

	// Updates
	Task<PropertyLetter> UpdateByIdAsync(PropertyLetter propertyLetter);

	// Deletes
	Task DeleteByPropertyIdAsync(Guid propertyId);
}

