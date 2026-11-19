using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Api.Services;

/// <summary>
/// Saves contact cards. When the office's payment integration is Number, the card is held by Number:
/// only the last four (and Number's details) are kept here, and the number is never encrypted or decrypted.
/// </summary>
public class ContactCardStore
{
    private readonly IContactRepository _contactRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IEncryptionService _encryptionService;

    public ContactCardStore(IContactRepository contactRepository, IOrganizationRepository organizationRepository, IEncryptionService encryptionService)
    {
        _contactRepository = contactRepository;
        _organizationRepository = organizationRepository;
        _encryptionService = encryptionService;
    }

    public async Task<bool> OfficeStoresCardsExternallyAsync(Guid organizationId, int officeId)
    {
        var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
        return accountingOffice?.PaymentIntegrationTypeId == (int)PaymentIntegrationType.Number;
    }

    /// <summary>Creates a card from a full number typed in by staff or an intake.</summary>
    public async Task<ContactCard> CreateAsync(ContactCard model, string cardNumber)
    {
        var external = await OfficeStoresCardsExternallyAsync(model.OrganizationId, model.OfficeId);
        model.LastFour = ExtractLastFour(cardNumber);
        model.IsExternallyStored = external;
        model.ExpirationDate = null;
        model.NumberConsentId = null;
        var encrypted = external ? null : await _encryptionService.EncryptAsync(cardNumber);
        return await _contactRepository.CreateContactCardAsync(model, encrypted);
    }

    /// <summary>
    /// Updates a card. A blank or masked number keeps the existing card; a new number replaces it,
    /// which also drops Number's token and expiration because they belonged to the old card.
    /// </summary>
    public async Task<ContactCard> UpdateAsync(ContactCard model, string? cardNumber, ContactCard existing)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Contains('*', StringComparison.Ordinal))
        {
            model.LastFour = existing.LastFour;
            model.IsExternallyStored = existing.IsExternallyStored;
            model.ExpirationDate = existing.ExpirationDate;
            model.NumberConsentId = existing.NumberConsentId;
            var kept = existing.IsExternallyStored || string.IsNullOrWhiteSpace(existing.CardNumber)
                ? null
                : Convert.FromBase64String(existing.CardNumber);
            return await _contactRepository.UpdateContactCardByIdAsync(model, kept);
        }

        var external = await OfficeStoresCardsExternallyAsync(model.OrganizationId, model.OfficeId);
        model.LastFour = ExtractLastFour(cardNumber);
        model.IsExternallyStored = external;
        model.ExpirationDate = null;
        model.NumberConsentId = null;
        var encrypted = external ? null : await _encryptionService.EncryptAsync(cardNumber);
        return await _contactRepository.UpdateContactCardByIdAsync(model, encrypted);
    }

    /// <summary>
    /// Saves the card Number kept on file after a payment, as the contact's card. Creates the card
    /// or replaces the existing one, and links it to the contact.
    /// </summary>
    public async Task<ContactCard> SaveNumberCardAsync(Contact contact, ContactCard numberCard, Guid modifiedBy)
    {
        numberCard.OrganizationId = contact.OrganizationId;
        numberCard.OfficeId = contact.OfficeId;
        numberCard.IsExternallyStored = true;

        ContactCard saved;
        var existing = contact.ContactCardId is > 0
            ? await _contactRepository.GetContactCardByIdAsync(contact.ContactCardId.Value, contact.OrganizationId, contact.OfficeId)
            : null;
        if (existing != null)
        {
            numberCard.ContactCardId = existing.ContactCardId;
            saved = await _contactRepository.UpdateContactCardByIdAsync(numberCard, null);
        }
        else
        {
            saved = await _contactRepository.CreateContactCardAsync(numberCard, null);
        }

        if (contact.ContactCardId != saved.ContactCardId)
        {
            contact.ContactCardId = saved.ContactCardId;
            contact.ModifiedBy = modifiedBy;
            await _contactRepository.UpdateByIdAsync(contact);
        }
        return saved;
    }

    public static string ExtractLastFour(string? cardNumber)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? digits : digits[^4..];
    }
}
