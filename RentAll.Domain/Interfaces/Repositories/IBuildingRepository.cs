using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IBuildingRepository
{
	// Creates
	Task<Building> CreateAsync(Building building);

	// Selects
	Task<IEnumerable<Building>> GetAllAsync(Guid organizationId);
	Task<Building?> GetByIdAsync(int buildingId, Guid organizationId);
	Task<Building?> GetByBuildingCodeAsync(string buildingCode, Guid organizationId);
	Task<bool> ExistsByBuildingCodeAsync(string buildingCode, Guid organizationId);

	// Updates
	Task<Building> UpdateByIdAsync(Building building);

	// Deletes
	Task DeleteByIdAsync(int buildingId);
}

