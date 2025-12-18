using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IColorRepository
{
	// Selects
	Task<IEnumerable<Colour>> GetAllAsync(Guid organizationId);
	Task<Colour?> GetByIdAsync(int colorId, Guid organizationId);

	// Updates
	Task UpdateByIdAsync(Colour color);
}

