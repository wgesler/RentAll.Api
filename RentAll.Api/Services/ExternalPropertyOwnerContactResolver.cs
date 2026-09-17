using RentAll.Api.Dtos.Properties.Properties;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace RentAll.Api.Services;

public class ExternalPropertyOwnerContactResolver
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactManager _contactManager;

    public ExternalPropertyOwnerContactResolver(IContactRepository contactRepository, IContactManager contactManager)
    {
        _contactRepository = contactRepository;
        _contactManager = contactManager;
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

        return true;
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
            return (true, updatedContact.ContactId, null);
        }

        var code = await _contactManager.GenerateContactCodeAsync(organizationId, (int)EntityType.Owner);
        var createdContact = await _contactRepository.CreateAsync(owner.ToNewOwnerContactModel(organizationId, officeId, code, currentUser));
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
            return (true, updatedContact.ContactId, null);
        }

        var code = await _contactManager.GenerateContactCodeAsync(organizationId, (int)EntityType.Vendor);
        var createdContact = await _contactRepository.CreateAsync(vendor.ToNewVendorContactModel(organizationId, officeId, code, currentUser));
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
