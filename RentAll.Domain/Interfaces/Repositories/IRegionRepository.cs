using RentAll.Domain.Models.Properties;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IRegionRepository
{
	// Creates
	Task<Region> CreateAsync(Region region);

	// Selects
	Task<IEnumerable<Region>> GetAllAsync(Guid OrganizationId);
	Task<Region?> GetByIdAsync(int regionId, Guid OrganizationId);
	Task<Region?> GetByRegionCodeAsync(string regionCode, Guid OrganizationId);
	Task<bool> ExistsByRegionCodeAsync(string regionCode, Guid OrganizationId);

	// Updates
	Task<Region> UpdateByIdAsync(Region region);

	// Deletes
	Task DeleteByIdAsync(int regionId);
}

