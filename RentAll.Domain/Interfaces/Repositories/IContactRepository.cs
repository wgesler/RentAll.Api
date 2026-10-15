using RentAll.Domain.Models;

namespace RentAll.Domain.Interfaces.Repositories;

public interface IContactRepository
{
    #region Contacts
    Task<IEnumerable<Contact>> GetContactsByOfficeIdAsync(Guid organizationId, string officeAccess);
    Task<IEnumerable<Contact>> GetExternalContactsByOfficeAsync(Guid organizationId, int officeId);
    Task<IEnumerable<Contact>> GetContactsByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<Contact>> GetContactsByContactTypeIdAsync(int contactTypeId, Guid organizationId);
    Task<Contact?> GetContactByIdAsync(Guid contactId);
    Task<Contact?> GetContactByIdsAsync(Guid contactId, Guid organizationId);
    Task<Contact?> GetContactByLeadAsync(Guid organizationId, string officeAccess, int? leadOwnerId, string? firstName, string? lastName, string? address);
    Task<Contact?> GetContactByEmailAsync(string email, Guid organizationId);
    Task<Contact?> GetContactByContactCodeAsync(string contactCode, Guid organizationId);
    Task<bool> ExistsByContactCodeAsync(string contactCode, Guid organizationId);

    Task<Contact> CreateAsync(Contact contact);
    Task<Contact> UpdateByIdAsync(Contact contact);
    Task SaveEncryptedAccountNumberAsync(Guid contactId, byte[] encryptedAccountNumber, string lastFour, Guid modifiedBy);
    Task<byte[]?> GetEncryptedAccountNumberAsync(Guid contactId);
    Task<IEnumerable<(Guid ContactId, string AccountNumber)>> GetClearAccountNumbersAsync(Guid organizationId);
    Task ClearBankInformationAsync(Guid contactId, Guid modifiedBy);
    Task DeleteContactByIdAsync(Guid contactId, Guid modifiedBy);
    #endregion

    #region ContactCards
    Task<ContactCard?> GetContactCardByIdAsync(int contactCardId, Guid organizationId, int officeId);
    Task<ContactCard> CreateContactCardAsync(ContactCard contactCard, byte[] encryptedCardNumber);
    Task<ContactCard> UpdateContactCardByIdAsync(ContactCard contactCard, byte[] encryptedCardNumber);
    Task DeleteContactCardByIdAsync(int contactCardId, Guid organizationId, int officeId);
    #endregion
}
