using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICostCodeRepository
{
	// Creates
	Task<CostCode> CreateAsync(CostCode costCode);

	// Selects
	Task<List<CostCode>> GetAllAsync(string officeIds, Guid organizationId);
	Task<List<CostCode>> GetAllByOfficeIdAsync(int officeId, Guid organizationId);
	Task<CostCode?> GetByIdAsync(int costCodeId, int officeId, Guid organizationId);
	Task<CostCode?> GetByCostCodeAsync(string costCode, int officeId, Guid organizationId);
	Task<bool> ExistsByCostCodeAsync(string costCode, int officeId, Guid organizationId);

	// Updates
	Task<CostCode> UpdateByIdAsync(CostCode costCode);

	// Deletes
	Task DeleteByIdAsync(int costCodeId, int officeId, Guid organizationId);
}
