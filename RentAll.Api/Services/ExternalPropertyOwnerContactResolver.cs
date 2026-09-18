using RentAll.Api.Dtos.Contacts.ContactCards;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using System.Text.Json;

namespace RentAll.Api.Services;

public class ExternalPropertyOwnerContactResolver
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactManager _contactManager;
    private readonly IEncryptionService _encryptionService;

    public ExternalPropertyOwnerContactResolver(IContactRepository contactRepository, IContactManager contactManager, IEncryptionService encryptionService)
    {
        _contactRepository = contactRepository;
        _contactManager = contactManager;
        _encryptionService = encryptionService;
    }

    public async Task<(bool Success, Guid? Owner1Id, Guid? Owner2Id, Guid? Owner3Id, Guid? PropertyVendorContactId, string? ErrorMessage)> ResolveContactsAsync(CreateExternalPropertyDto dto, ExternalPropertyIntakeContext context, Guid currentUser)
    {
        var leaseType = (PropertyLeaseType)dto.PropertyLeaseTypeId;
        Guid? owner1Id = null;
        Guid? owner2Id = null;
        Guid? owner3Id = null;
        Guid? propertyVendorContactId = null;

        if (leaseType is PropertyLeaseType.Direct or PropertyLeaseType.ThirdParty)
        {
            var vendorResult = await ResolveVendorAsync(dto.Vendor!, context.OrganizationId, context.OfficeId, currentUser);
            if (!vendorResult.Success)
                return (false, null, null, null, null, vendorResult.ErrorMessage);
            propertyVendorContactId = vendorResult.ContactId;
        }

        if (dto.Owner1 != null)
        {
            var owner1Result = await ResolveOwnerAsync(dto.Owner1, context.OrganizationId, context.OfficeId, currentUser, "Owner1");
            if (!owner1Result.Success)
                return (false, null, null, null, null, owner1Result.ErrorMessage);
            owner1Id = owner1Result.ContactId;
        }

        if (dto.Owner2 != null)
        {
            var owner2Result = await ResolveOwnerAsync(dto.Owner2, context.OrganizationId, context.OfficeId, currentUser, "Owner2");
            if (!owner2Result.Success)
                return (false, null, null, null, null, owner2Result.ErrorMessage);
            owner2Id = owner2Result.ContactId;
        }

        if (dto.Owner3 != null)
        {
            var owner3Result = await ResolveOwnerAsync(dto.Owner3, context.OrganizationId, context.OfficeId, currentUser, "Owner3");
            if (!owner3Result.Success)
                return (false, null, null, null, null, owner3Result.ErrorMessage);
            owner3Id = owner3Result.ContactId;
        }

        return (true, owner1Id, owner2Id, owner3Id, propertyVendorContactId, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> TryApplyContactPatchesAsync(JsonElement body, ExternalPropertyIntakeContext context, UpdatePropertyDto updateDto, Guid currentUser)
    {
        if (TryGetContact(body, "owner1", out var owner1))
        {
            var owner1Result = await ResolveOwnerAsync(owner1, context.OrganizationId, context.OfficeId, currentUser, "Owner1");
            if (!owner1Result.Success)
                return (false, owner1Result.ErrorMessage);
            updateDto.Owner1Id = owner1Result.ContactId;
        }

        if (TryGetContact(body, "owner2", out var owner2))
        {
            var owner2Result = await ResolveOwnerAsync(owner2, context.OrganizationId, context.OfficeId, currentUser, "Owner2");
            if (!owner2Result.Success)
                return (false, owner2Result.ErrorMessage);
            updateDto.Owner2Id = owner2Result.ContactId;
        }

        if (TryGetContact(body, "owner3", out var owner3))
        {
            var owner3Result = await ResolveOwnerAsync(owner3, context.OrganizationId, context.OfficeId, currentUser, "Owner3");
            if (!owner3Result.Success)
                return (false, owner3Result.ErrorMessage);
            updateDto.Owner3Id = owner3Result.ContactId;
        }

        if (TryGetContact(body, "vendor", out var vendor))
        {
            var vendorResult = await ResolveVendorAsync(vendor, context.OrganizationId, context.OfficeId, currentUser);
            if (!vendorResult.Success)
                return (false, vendorResult.ErrorMessage);
            updateDto.VendorId = vendorResult.ContactId;
        }

        return (true, null);
    }

    public static bool TryGetContact(JsonElement body, string fieldName, out ExternalPropertyContactDto contact)
    {
        contact = new ExternalPropertyContactDto();
        if (body.ValueKind != JsonValueKind.Object)
            return false;

        if (!TryGetProperty(body, fieldName, out var contactElement) || contactElement.ValueKind != JsonValueKind.Object)
            return false;

        contact.FirstName = TryGetTrimmedString(contactElement, "firstName") ?? string.Empty;
        contact.LastName = TryGetTrimmedString(contactElement, "lastName") ?? string.Empty;
        contact.Email = TryGetTrimmedString(contactElement, "email") ?? string.Empty;
        contact.Phone = TryGetTrimmedString(contactElement, "phone");
        contact.Address1 = TryGetTrimmedString(contactElement, "address1");
        contact.Address2 = TryGetTrimmedString(contactElement, "address2");
        contact.City = TryGetTrimmedString(contactElement, "city");
        contact.State = TryGetTrimmedString(contactElement, "state");
        contact.Zip = TryGetTrimmedString(contactElement, "zip");
        contact.CompanyName = TryGetTrimmedString(contactElement, "companyName");
        if (TryGetProperty(contactElement, "ownerTypeId", out var ownerTypeElement) && TryCoerceInt32(ownerTypeElement, out var ownerTypeId))
            contact.OwnerTypeId = ownerTypeId;

        contact.ContactCard = TryGetContactCard(contactElement);

        return true;
    }

    private async Task ApplyContactCardAsync(Contact contact, UpsertContactCardDto? card, Guid currentUser)
    {
        if (card == null || card.IsEmpty())
            return;

        int? cardId;
        if (contact.ContactCardId is > 0)
        {
            var existing = await _contactRepository.GetContactCardByIdAsync(contact.ContactCardId.Value, contact.OrganizationId, contact.OfficeId);
            if (existing == null)
                cardId = await CreateContactCardAsync(contact.OrganizationId, contact.OfficeId, card);
            else
            {
                var updateDto = new UpdateContactCardDto
                {
                    ContactCardId = existing.ContactCardId,
                    CardTypeId = card.CardTypeId,
                    CardName = card.CardName,
                    CardNumber = card.CardNumber
                };
                var model = updateDto.ToModel(contact.OrganizationId, contact.OfficeId);
                byte[] encrypted;
                if (string.IsNullOrWhiteSpace(card.CardNumber) || card.CardNumber.Contains('*', StringComparison.Ordinal))
                {
                    encrypted = Convert.FromBase64String(existing.CardNumber);
                    model.LastFour = existing.LastFour;
                }
                else
                {
                    model.LastFour = ExtractLastFour(card.CardNumber);
                    encrypted = await _encryptionService.EncryptAsync(card.CardNumber);
                }

                var updated = await _contactRepository.UpdateContactCardByIdAsync(model, encrypted);
                cardId = updated.ContactCardId;
            }
        }
        else
        {
            cardId = await CreateContactCardAsync(contact.OrganizationId, contact.OfficeId, card);
        }

        if (contact.ContactCardId == cardId)
            return;

        contact.ContactCardId = cardId;
        contact.ModifiedBy = currentUser;
        await _contactRepository.UpdateByIdAsync(contact);
    }

    private async Task<int> CreateContactCardAsync(Guid organizationId, int officeId, UpsertContactCardDto card)
    {
        var createDto = new CreateContactCardDto { CardTypeId = card.CardTypeId, CardName = card.CardName, CardNumber = card.CardNumber };
        var model = createDto.ToModel(organizationId, officeId);
        model.LastFour = ExtractLastFour(model.CardNumber);
        var encrypted = await _encryptionService.EncryptAsync(model.CardNumber);
        var created = await _contactRepository.CreateContactCardAsync(model, encrypted);
        return created.ContactCardId;
    }

    private static UpsertContactCardDto? TryGetContactCard(JsonElement contactElement)
    {
        if (!TryGetProperty(contactElement, "contactCard", out var cardElement) || cardElement.ValueKind != JsonValueKind.Object)
            return null;

        var card = new UpsertContactCardDto
        {
            CardName = TryGetTrimmedString(cardElement, "cardName") ?? string.Empty,
            CardNumber = TryGetTrimmedString(cardElement, "cardNumber") ?? string.Empty
        };

        if (TryGetProperty(cardElement, "cardTypeId", out var typeElement) && TryCoerceInt32(typeElement, out var cardTypeId))
            card.CardTypeId = cardTypeId;

        return card.IsEmpty() ? null : card;
    }

    private static string ExtractLastFour(string cardNumber)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? digits : digits[^4..];
    }

    public static (bool Success, ExternalPropertyIntakeContext? Context, string? ErrorMessage) TryParseIntakeContext(JsonElement body)
        => ExternalPropertyIntakeJson.TryParseIntakeContext(body);

    public static (bool Success, ExternalPropertyKeyDto? Keys, string? ErrorMessage) TryParsePropertyKeys(JsonElement propertyBody, ExternalPropertyIntakeContext context)
    {
        if (propertyBody.ValueKind != JsonValueKind.Object)
            return (false, null, "Property data is required");

        if (!TryGetProperty(propertyBody, "propertyCode", out var propertyCodeElement) || propertyCodeElement.ValueKind != JsonValueKind.String)
            return (false, null, "PropertyCode is required");

        var propertyCode = propertyCodeElement.GetString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(propertyCode))
            return (false, null, "PropertyCode is required");

        return (true, new ExternalPropertyKeyDto
        {
            OrganizationId = context.OrganizationId,
            OfficeId = context.OfficeId,
            VendorId = context.PartnerVendorId,
            PropertyCode = propertyCode
        }, null);
    }

    private async Task<(bool Success, Guid? ContactId, string? ErrorMessage)> ResolveOwnerAsync(ExternalPropertyContactDto owner, Guid organizationId, int officeId, Guid currentUser, string fieldLabel)
    {
        var (isValid, validationError) = owner.IsValid(fieldLabel);
        if (!isValid)
            return (false, null, validationError);

        var email = owner.Email.Trim();
        var existingContact = await _contactRepository.GetContactByEmailAsync(email, organizationId);
        if (existingContact != null)
        {
            if (existingContact.EntityType != EntityType.Owner)
                return (false, null, $"{fieldLabel} email '{email}' is already used by {existingContact.EntityType} contact {existingContact.ContactCode}. Use a different email or change that contact to Owner.");

            owner.ApplyToExistingOwnerContact(existingContact, officeId, currentUser);
            var updatedContact = await _contactRepository.UpdateByIdAsync(existingContact);
            await ApplyContactCardAsync(updatedContact, owner.ContactCard, currentUser);
            return (true, updatedContact.ContactId, null);
        }

        var code = await _contactManager.GenerateContactCodeAsync(organizationId, (int)EntityType.Owner);
        var createdContact = await _contactRepository.CreateAsync(owner.ToNewOwnerContactModel(organizationId, officeId, code, currentUser));
        await ApplyContactCardAsync(createdContact, owner.ContactCard, currentUser);
        return (true, createdContact.ContactId, null);
    }

    private async Task<(bool Success, Guid? ContactId, string? ErrorMessage)> ResolveVendorAsync(ExternalPropertyContactDto vendor, Guid organizationId, int officeId, Guid currentUser)
    {
        var (isValid, validationError) = vendor.IsValid("Vendor");
        if (!isValid)
            return (false, null, validationError);

        var email = vendor.Email.Trim();
        var existingContact = await _contactRepository.GetContactByEmailAsync(email, organizationId);
        if (existingContact != null)
        {
            if (existingContact.EntityType != EntityType.Vendor)
                return (false, null, $"Vendor email '{email}' is already used by {existingContact.EntityType} contact {existingContact.ContactCode}. Use a different email or change that contact to Vendor.");

            vendor.ApplyToExistingVendorContact(existingContact, officeId, currentUser);
            var updatedContact = await _contactRepository.UpdateByIdAsync(existingContact);
            await ApplyContactCardAsync(updatedContact, vendor.ContactCard, currentUser);
            return (true, updatedContact.ContactId, null);
        }

        var code = await _contactManager.GenerateContactCodeAsync(organizationId, (int)EntityType.Vendor);
        var createdContact = await _contactRepository.CreateAsync(vendor.ToNewVendorContactModel(organizationId, officeId, code, currentUser));
        await ApplyContactCardAsync(createdContact, vendor.ContactCard, currentUser);
        return (true, createdContact.ContactId, null);
    }

    private static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
    {
        foreach (var property in body.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? TryGetTrimmedString(JsonElement body, string name)
    {
        if (!TryGetProperty(body, name, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        var value = element.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool TryCoerceInt32(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        return element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString()?.Trim(), out value);
    }
}
