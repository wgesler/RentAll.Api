using RentAll.Domain.Models.Companies;

namespace RentAll.Domain.Interfaces.Repositories;

public interface ICompanyContactRepository
{
	// Creates
	Task<CompanyContact> CreateAsync(CompanyContact companyContact);

	// Selects
	Task<CompanyContact?> GetByIdAsync(Guid contactId);
	Task<IEnumerable<CompanyContact>> GetByCompanyIdAsync(Guid companyId);

	// Updates
	Task<CompanyContact> UpdateByIdAsync(CompanyContact companyContact);

	// Deletes
	Task DeleteByIdAsync(Guid contactId);
}



