using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IFranchiseRepository
{
	// Creates
	Task<Franchise> CreateAsync(Franchise franchise);

	// Selects
	Task<IEnumerable<Franchise>> GetAllAsync(Guid organizationId);
	Task<Franchise?> GetByIdAsync(int franchiseId, Guid organizationId);
	Task<Franchise?> GetByFranchiseCodeAsync(string franchiseCode, Guid organizationId);
	Task<bool> ExistsByFranchiseCodeAsync(string franchiseCode, Guid organizationId);

	// Updates
	Task<Franchise> UpdateByIdAsync(Franchise franchise);

	// Deletes
	Task DeleteByIdAsync(int franchiseId);
}



