using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAreaRepository
{
	// Creates
	Task<Area> CreateAsync(Area area);

	// Selects
	Task<IEnumerable<Area>> GetAllAsync(Guid organizationId);
	Task<Area?> GetByIdAsync(int areaId, Guid organizationId);
	Task<Area?> GetByAreaCodeAsync(string areaCode, Guid organizationId);
	Task<bool> ExistsByAreaCodeAsync(string areaCode, Guid organizationId);

	// Updates
	Task<Area> UpdateByIdAsync(Area area);

	// Deletes
	Task DeleteByIdAsync(int areaId);
}



