using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyContactDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public int? OwnerTypeId { get; set; }
    public string? CompanyName { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            return (false, $"{fieldLabel}.FirstName is required");

        if (string.IsNullOrWhiteSpace(LastName))
            return (false, $"{fieldLabel}.LastName is required");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, $"{fieldLabel}.Email is required");

        if (string.Equals(fieldLabel, "Vendor", StringComparison.OrdinalIgnoreCase))
            return (true, null);

        if (OwnerTypeId.HasValue && !Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            return (false, $"{fieldLabel}.OwnerTypeId is invalid");

        return (true, null);
    }

    public Contact ToNewOwnerContactModel(Guid organizationId, int officeId, string contactCode, Guid currentUser)
    {
        var ownerType = OwnerTypeId.HasValue ? (OwnerType)OwnerTypeId.Value : OwnerType.Individual;
        return new Contact
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            OfficeAccess = new List<int> { officeId },
            ContactCode = contactCode,
            EntityType = EntityType.Owner,
            OwnerType = ownerType,
            VendorType = VendorType.Individual,
            Properties = new List<string>(),
            CompanyName = TrimOrNull(CompanyName),
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Email = Email.Trim(),
            Phone = TrimOrNull(Phone),
            Address1 = TrimOrNull(Address1),
            Address2 = TrimOrNull(Address2),
            City = TrimOrNull(City),
            State = TrimOrNull(State),
            Zip = TrimOrNull(Zip),
            Rating = 0,
            IsInternational = false,
            Markup = 25,
            RevenueSplitOwner = 75,
            RevenueSplitOffice = 25,
            WorkingCapitalBalance = 0,
            LinenAndTowelFee = 0,
            IsActive = true,
            CreatedBy = currentUser
        };
    }

    public Contact ToNewVendorContactModel(Guid organizationId, int officeId, string contactCode, Guid currentUser)
    {
        return new Contact
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            OfficeAccess = new List<int> { officeId },
            ContactCode = contactCode,
            EntityType = EntityType.Vendor,
            OwnerType = OwnerType.Individual,
            VendorType = VendorType.Company,
            Properties = new List<string>(),
            CompanyName = TrimOrNull(CompanyName),
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Email = Email.Trim(),
            Phone = TrimOrNull(Phone),
            Address1 = TrimOrNull(Address1),
            Address2 = TrimOrNull(Address2),
            City = TrimOrNull(City),
            State = TrimOrNull(State),
            Zip = TrimOrNull(Zip),
            Rating = 0,
            IsInternational = false,
            IsActive = true,
            CreatedBy = currentUser
        };
    }

    public void ApplyToExistingVendorContact(Contact contact, int officeId, Guid modifiedBy)
    {
        contact.OfficeId = officeId;
        contact.OfficeAccess = contact.OfficeAccess != null && contact.OfficeAccess.Count > 0
            ? contact.OfficeAccess.Distinct().ToList()
            : new List<int> { officeId };
        if (!contact.OfficeAccess.Contains(officeId))
            contact.OfficeAccess.Add(officeId);

        contact.FirstName = FirstName.Trim();
        contact.LastName = LastName.Trim();
        contact.Email = Email.Trim();
        contact.Phone = TrimOrNull(Phone) ?? contact.Phone;
        contact.Address1 = TrimOrNull(Address1) ?? contact.Address1;
        contact.Address2 = TrimOrNull(Address2) ?? contact.Address2;
        contact.City = TrimOrNull(City) ?? contact.City;
        contact.State = TrimOrNull(State) ?? contact.State;
        contact.Zip = TrimOrNull(Zip) ?? contact.Zip;
        if (!string.IsNullOrWhiteSpace(CompanyName))
            contact.CompanyName = CompanyName.Trim();
        contact.ModifiedBy = modifiedBy;
    }

    public void ApplyToExistingOwnerContact(Contact contact, int officeId, Guid modifiedBy)
    {
        contact.OfficeId = officeId;
        contact.OfficeAccess = contact.OfficeAccess != null && contact.OfficeAccess.Count > 0
            ? contact.OfficeAccess.Distinct().ToList()
            : new List<int> { officeId };
        if (!contact.OfficeAccess.Contains(officeId))
            contact.OfficeAccess.Add(officeId);

        contact.FirstName = FirstName.Trim();
        contact.LastName = LastName.Trim();
        contact.Email = Email.Trim();
        contact.Phone = TrimOrNull(Phone) ?? contact.Phone;
        contact.Address1 = TrimOrNull(Address1) ?? contact.Address1;
        contact.Address2 = TrimOrNull(Address2) ?? contact.Address2;
        contact.City = TrimOrNull(City) ?? contact.City;
        contact.State = TrimOrNull(State) ?? contact.State;
        contact.Zip = TrimOrNull(Zip) ?? contact.Zip;
        if (OwnerTypeId.HasValue && Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            contact.OwnerType = (OwnerType)OwnerTypeId.Value;
        if (!string.IsNullOrWhiteSpace(CompanyName))
            contact.CompanyName = CompanyName.Trim();
        contact.ModifiedBy = modifiedBy;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
