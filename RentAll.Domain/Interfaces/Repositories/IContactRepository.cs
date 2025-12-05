using RentAll.Domain.Models.Contacts;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IContactRepository
{
    // Creates
    Task<Contact> CreateAsync(Contact contact);

    // Selects
    Task<Contact?> GetByIdAsync(Guid contactId);
    Task<Contact?> GetByContactCodeAsync(string contactCode);
    Task<IEnumerable<Contact>> GetByContactTypeIdAsync(int contactTypeId);
    Task<bool> ExistsByContactCodeAsync(string contactCode);

    // Updates
    Task<Contact> UpdateByIdAsync(Contact contact);

    // Deletes
    Task DeleteByIdAsync(Guid contactId);
}