using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOfficeConfigurationRepository
{
	// Creates
	Task<OfficeConfiguration> CreateAsync(OfficeConfiguration officeConfiguration);

	// Selects
	Task<IEnumerable<OfficeConfiguration>> GetAllAsync(Guid organizationId);
	Task<OfficeConfiguration?> GetByOfficeIdAsync(int officeId, Guid organizationId);

	// Updates
	Task<OfficeConfiguration> UpdateByOfficeIdAsync(OfficeConfiguration officeConfiguration);

	// Deletes
	Task DeleteByOfficeIdAsync(int officeId);
}


