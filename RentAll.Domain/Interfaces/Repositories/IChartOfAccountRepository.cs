using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IChartOfAccountRepository
{
	// Creates
	Task<ChartOfAccount> CreateAsync(ChartOfAccount chartOfAccount);

	// Selects
	Task<List<ChartOfAccount>> GetAllByOfficeIdAsync(int officeId, Guid organizationId);
	Task<ChartOfAccount?> GetByIdAsync(int chartOfAccountId, int officeId, Guid organizationId);
	Task<ChartOfAccount?> GetByAccountNumberAsync(int accountNumber, int officeId, Guid organizationId);
	Task<bool> ExistsByAccountNumberAsync(string accountNumber, int officeId, Guid organizationId);

	// Updates
	Task<ChartOfAccount> UpdateByIdAsync(ChartOfAccount chartOfAccount);

	// Deletes
	Task DeleteByIdAsync(int chartOfAccountId, int officeId, Guid organizationId);
}
