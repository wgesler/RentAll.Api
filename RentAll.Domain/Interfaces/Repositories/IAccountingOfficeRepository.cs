using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IAccountingOfficeRepository
{
	// Creates
	Task<AccountingOffice> CreateAsync(AccountingOffice accountingOffice);

	// Selects
	Task<IEnumerable<AccountingOffice>> GetAllAsync(Guid organizationId);
	Task<AccountingOffice?> GetByIdAsync(Guid organizationId, int officeId);

	// Updates
	Task<AccountingOffice> UpdateAsync(AccountingOffice accountingOffice);

	// Deletes
	Task DeleteAsync(Guid organizationId, int officeId);
}
