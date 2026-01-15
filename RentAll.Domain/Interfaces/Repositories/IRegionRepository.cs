using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IRegionRepository
{
	// Creates
	Task<Region> CreateAsync(Region region);

	// Selects
	Task<IEnumerable<Region>> GetAllAsync(Guid OrganizationId);
	Task<IEnumerable<Region>> GetAllByOfficeIdAsync(Guid OrganizationId, string officeAccess);
	Task<Region?> GetByIdAsync(int regionId, Guid OrganizationId);
	Task<Region?> GetByRegionCodeAsync(string regionCode, Guid OrganizationId, int? officeId);
	Task<bool> ExistsByRegionCodeAsync(string regionCode, Guid OrganizationId, int? officeId);

	// Updates
	Task<Region> UpdateByIdAsync(Region region);

	// Deletes
	Task DeleteByIdAsync(int regionId);
}




