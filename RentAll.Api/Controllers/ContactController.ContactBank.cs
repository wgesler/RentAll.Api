using RentAll.Api.Dtos.Contacts;
using RentAll.Api.Dtos.Contacts.ContactBanks;

namespace RentAll.Api.Controllers;

public partial class ContactController
{
    #region Contact Bank Private Methods
    private async Task AttachContactBankAsync(ContactResponseDto response, Contact contact)
    {
        if (contact.ContactBankId is not > 0)
            return;

        var bank = await _contactRepository.GetContactBankByIdAsync(contact.ContactBankId.Value, contact.OrganizationId, contact.OfficeId);
        if (bank == null)
            return;

        response.ContactBankId = bank.ContactBankId;
        response.ContactBank = new ContactBankResponseDto(bank);
    }

    /// <summary>The contact's ContactBankId after applying the bank from the contact screen; no bank sent keeps the existing one.</summary>
    private async Task<int?> SyncContactBankAsync(Guid organizationId, int officeId, int? existingBankId, UpsertContactBankDto? bank)
    {
        if (bank == null)
            return existingBankId is > 0 ? existingBankId : null;

        if (bank.IsEmpty())
        {
            if (existingBankId is > 0)
                await _contactRepository.DeleteContactBankByIdAsync(existingBankId.Value, organizationId, officeId);

            return null;
        }

        var saved = await _contactBankStore.UpsertAsync(organizationId, officeId, existingBankId, bank.ToModel(), bank.AccountNumber);
        return saved?.ContactBankId;
    }
    #endregion
}
