using RentAll.Domain.Models.Companies;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICompanyRepository
{
	// Creates
	Task<Company> CreateAsync(Company company);

	// Selects
	Task<Company?> GetByIdAsync(Guid companyId);
	Task<IEnumerable<Company>> GetAllAsync();
	Task<bool> ExistsByCompanyCodeAsync(string companyCode);

	// Updates
	Task<Company> UpdateByIdAsync(Company company);

	// Deletes
	Task DeleteByIdAsync(Guid companyId);
}