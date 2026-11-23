using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Services;

/// <summary>
/// Saves contact bank accounts (Organization.ContactBank), linked from Contact.ContactBankId like ContactCard.
/// The account number is always encrypted; a blank or masked number keeps the stored one. A new routing or account
/// number drops the Number consent, because it belonged to the old account.
/// </summary>
public class ContactBankStore
{
    private readonly IContactRepository _contactRepository;
    private readonly IEncryptionService _encryptionService;

    public ContactBankStore(IContactRepository contactRepository, IEncryptionService encryptionService)
    {
        _contactRepository = contactRepository;
        _encryptionService = encryptionService;
    }

    /// <summary>Creates or updates the bank row; the caller links it to the contact.</summary>
    public async Task<ContactBank> UpsertAsync(Guid organizationId, int officeId, int? existingBankId, ContactBank model, string? accountNumber)
    {
        model.OrganizationId = organizationId;
        model.OfficeId = officeId;
        var existing = existingBankId is > 0 ? await _contactRepository.GetContactBankByIdAsync(existingBankId.Value, organizationId, officeId) : null;

        byte[]? encrypted;
        var digits = ClearAccountDigits(accountNumber);
        if (digits == null)
        {
            encrypted = existing == null || string.IsNullOrWhiteSpace(existing.AccountNumber) ? null : Convert.FromBase64String(existing.AccountNumber);
            model.LastFour = existing?.LastFour;
        }
        else
        {
            encrypted = await _encryptionService.EncryptAsync(digits);
            model.LastFour = digits[^4..];
        }

        // Keep the consent while the account is the same one; a new consent passed in always wins.
        if (model.NumberConsentId == null && existing != null && existing.RoutingNumber == model.RoutingNumber && existing.LastFour == model.LastFour)
        {
            model.NumberConsentId = existing.NumberConsentId;
            model.ReservationId = existing.ReservationId;
            model.AuthorizedOn = existing.AuthorizedOn;
        }

        if (existing == null)
            return await _contactRepository.CreateContactBankAsync(model, encrypted);

        model.ContactBankId = existing.ContactBankId;
        return await _contactRepository.UpdateContactBankByIdAsync(model, encrypted);
    }

    /// <summary>Saves the contact's bank account and links it to the contact.</summary>
    public async Task<ContactBank> SaveAsync(Contact contact, ContactBank model, string? accountNumber, Guid modifiedBy)
    {
        var saved = await UpsertAsync(contact.OrganizationId, contact.OfficeId, contact.ContactBankId, model, accountNumber);
        if (contact.ContactBankId != saved.ContactBankId)
        {
            contact.ContactBankId = saved.ContactBankId;
            contact.ModifiedBy = modifiedBy;
            await _contactRepository.UpdateByIdAsync(contact);
        }
        return saved;
    }

    /// <summary>
    /// Bank fields from an external intake (property or reservation API): fields that were sent replace the stored ones,
    /// fields left out keep their values. Nothing sent means nothing changes. Without a stored value, the account type
    /// defaults to personal checking and the name on the account to the contact's first and last name.
    /// </summary>
    public async Task ApplyExternalAsync(Contact contact, string? bankName, string? routingNumber, string? accountNumber, int? bankAccountTypeId, string? nameOnAccount, Guid modifiedBy)
    {
        bankName = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim();
        routingNumber = string.IsNullOrWhiteSpace(routingNumber) ? null : routingNumber.Trim();
        nameOnAccount = string.IsNullOrWhiteSpace(nameOnAccount) ? null : nameOnAccount.Trim();
        var digits = ClearAccountDigits(accountNumber);
        if (bankName == null && routingNumber == null && digits == null && bankAccountTypeId == null && nameOnAccount == null)
            return;

        var bank = contact.ContactBankId is > 0 ? await _contactRepository.GetContactBankByIdAsync(contact.ContactBankId.Value, contact.OrganizationId, contact.OfficeId) : null;
        bank ??= new ContactBank();
        bank.BankName = bankName ?? bank.BankName;
        bank.RoutingNumber = routingNumber ?? bank.RoutingNumber;
        bank.BankAccountTypeId = bankAccountTypeId ?? (bank.BankAccountTypeId != (int)BankAccountType.Unknown ? bank.BankAccountTypeId : (int)BankAccountType.PersonalChecking);
        bank.NameOnAccount = nameOnAccount ?? bank.NameOnAccount ?? DefaultNameOnAccount(contact);
        await SaveAsync(contact, bank, digits, modifiedBy);
    }

    private static string? DefaultNameOnAccount(Contact contact)
    {
        var name = string.Join(' ', new[] { contact.FirstName, contact.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
        return name.Length == 0 ? null : name.Length > 100 ? name[..100] : name;
    }

    /// <summary>Removes the contact's bank account and clears the link.</summary>
    public async Task DeleteAsync(Contact contact, Guid modifiedBy)
    {
        if (contact.ContactBankId is not > 0)
            return;

        await _contactRepository.DeleteContactBankByIdAsync(contact.ContactBankId.Value, contact.OrganizationId, contact.OfficeId);
        contact.ContactBankId = null;
        contact.ModifiedBy = modifiedBy;
        await _contactRepository.UpdateByIdAsync(contact);
    }

    /// <summary>The contact's full account number, decrypted; empty when none is stored.</summary>
    public async Task<string> RevealAccountNumberAsync(Contact contact)
    {
        if (contact.ContactBankId is not > 0)
            return string.Empty;

        var bank = await _contactRepository.GetContactBankByIdAsync(contact.ContactBankId.Value, contact.OrganizationId, contact.OfficeId);
        if (bank == null || string.IsNullOrWhiteSpace(bank.AccountNumber))
            return string.Empty;

        return await _encryptionService.DecryptAsync(Convert.FromBase64String(bank.AccountNumber));
    }

    /// <summary>The digits of a typed account number, or null for blank, masked (****1234) or invalid input.</summary>
    public static string? ClearAccountDigits(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Contains('*', StringComparison.Ordinal))
            return null;

        var compact = accountNumber.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return compact.Length >= 4 && compact.All(char.IsDigit) ? compact : null;
    }
}
