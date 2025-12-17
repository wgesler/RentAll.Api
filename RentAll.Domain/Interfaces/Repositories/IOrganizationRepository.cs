using RentAll.Domain.Models.Organizations;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
	// Creates
	Task<Organization> CreateAsync(Organization organization);

	// Selects
	Task<IEnumerable<Organization>> GetAllAsync();
	Task<Organization?> GetByIdAsync(Guid organizationId);
	Task<Organization?> GetByOrganizationCodeAsync(string organizationCode);
	Task<bool> ExistsByOrganizationCodeAsync(string organizationCode);

	// Updates
	Task<Organization> UpdateByIdAsync(Organization organization);
	Task<Organization> UpdateColorAsync(Guid organizationId, string rgb, Guid modifiedBy);

	// Deletes
	Task DeleteByIdAsync(Guid organizationId);
}


