using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IChartOfAccountRepository
{
	// Creates
	Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount);

	// Selects
	Task<IEnumerable<ChartOfAccount>> GetAllAsync(Guid organizationId);
	Task<ChartOfAccount?> GetByIdAsync(int chartOfAccountId, Guid organizationId);
	Task<ChartOfAccount?> GetByAccountNumberAsync(string accountNumber, Guid organizationId);
	Task<bool> ExistsByAccountNumberAsync(string accountNumber, Guid organizationId);

	// Updates
	Task<ChartOfAccount> UpdateByIdAsync(ChartOfAccount chartOfAccount);

	// Deletes
	Task DeleteByIdAsync(int chartOfAccountId, Guid organizationId);
}
