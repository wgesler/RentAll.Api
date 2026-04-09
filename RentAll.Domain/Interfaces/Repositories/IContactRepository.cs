using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IContactRepository
{
    #region Contacts
    Task<IEnumerable<Contact>> GetContactsByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Contact>> GetContactsByContactTypeIdAsync(int contactTypeId, Guid organizationId);
    Task<Contact?> GetContactByIdAsync(Guid contactId, Guid organizationId);
    Task<Contact?> GetContactByEmailAsync(string email, Guid organizationId);
    Task<Contact?> GetContactByContactCodeAsync(string contactCode, Guid organizationId);
    Task<bool> ExistsByContactCodeAsync(string contactCode, Guid organizationId);

    Task<Contact> CreateAsync(Contact contact);
    Task<Contact> UpdateByIdAsync(Contact contact);
    Task DeleteContactByIdAsync(Guid contactId, Guid modifiedBy);
    #endregion
}
