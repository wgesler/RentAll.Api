using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IOfficeRepository
{
	// Creates
	Task<Office> CreateAsync(Office office);

	// Selects
	Task<IEnumerable<Office>> GetAllAsync(Guid organizationId);
	Task<Office?> GetByIdAsync(int officeId, Guid organizationId);
	Task<Office?> GetByOfficeCodeAsync(string officeCode, Guid organizationId);
	Task<bool> ExistsByOfficeCodeAsync(string officeCode, Guid organizationId);

	// Updates
	Task<Office> UpdateByIdAsync(Office office);

	// Deletes
	Task DeleteByIdAsync(int officeId);
}

