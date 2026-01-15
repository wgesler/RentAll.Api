using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IContactRepository
{
    // Creates
    Task<Contact> CreateAsync(Contact contact);

	// Selects
	Task<IEnumerable<Contact>> GetAllAsync(Guid organizationId);
	Task<IEnumerable<Contact>> GetAllByOfficeIdAsync(Guid organizationId, string officeAccess);
	Task<Contact?> GetByIdAsync(Guid contactId, Guid organizationId);
    Task<Contact?> GetByContactCodeAsync(string contactCode, Guid organizationId);
    Task<IEnumerable<Contact>> GetByContactTypeIdAsync(int contactTypeId, Guid organizationId);
    Task<bool> ExistsByContactCodeAsync(string contactCode, Guid organizationId);

    // Updates
    Task<Contact> UpdateByIdAsync(Contact contact);

    // Deletes
    Task DeleteByIdAsync(Guid contactId);
}






