using RentAll.Domain.Models.Colors;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IColorRepository
{
	// Selects
	Task<IEnumerable<Colour>> GetAllAsync(Guid organizationId);
	Task<Colour?> GetByIdAsync(int colorId, Guid organizationId);

	// Updates
	Task<Colour> UpdateByIdAsync(Colour color);
}

