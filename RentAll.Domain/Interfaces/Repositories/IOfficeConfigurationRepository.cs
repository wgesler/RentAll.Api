using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOfficeConfigurationRepository
{
	// Creates
	Task<OfficeConfiguration> CreateAsync(OfficeConfiguration officeConfiguration);

	// Selects
	Task<OfficeConfiguration?> GetByOfficeIdAsync(int officeId);

	// Updates
	Task<OfficeConfiguration> UpdateByOfficeIdAsync(OfficeConfiguration officeConfiguration);

	// Deletes
	Task DeleteByOfficeIdAsync(int officeId);
}


