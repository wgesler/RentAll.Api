using RentAll.Domain.Models.Companies;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICompanyRepository
{
	// Creates
	Task<Company> CreateAsync(Company company);

	// Selects
	Task<IEnumerable<Company>> GetAllAsync(Guid OrganizationId);
	Task<Company?> GetByIdAsync(Guid companyId, Guid OrganizationId);
	Task<Company?> GetByCompanyCodeAsync(string companyCode, Guid OrganizationId);
	Task<bool> ExistsByCompanyCodeAsync(string companyCode, Guid OrganizationId);

	// Updates
	Task<Company> UpdateByIdAsync(Company company);

	// Deletes
	Task DeleteByIdAsync(Guid companyId);
}





