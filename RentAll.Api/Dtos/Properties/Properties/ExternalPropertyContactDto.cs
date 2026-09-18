using RentAll.Api.Dtos.Contacts.ContactCards;
using RentAll.Domain;

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
    public UpsertContactCardDto? ContactCard { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string fieldLabel)
    {
        var errors = CollectErrors(fieldLabel);
        return errors.Count == 0 ? (true, null) : (false, ExternalPropertyIntakeErrors.Join(errors));
    }

    public List<string> CollectErrors(string fieldLabel)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(FirstName))
            errors.Add($"{fieldLabel}.firstName is required.");
        if (string.IsNullOrWhiteSpace(LastName))
            errors.Add($"{fieldLabel}.lastName is required.");
        if (string.IsNullOrWhiteSpace(Email))
            errors.Add($"{fieldLabel}.email is required.");
        if (!string.IsNullOrWhiteSpace(State) && !UsStateCode.IsRecognized(State))
            errors.Add($"{fieldLabel}.state must be a 2-letter US code or full state name. Received \"{State}\".");
        if (!string.IsNullOrWhiteSpace(Zip) && Zip.Trim().Length > 10)
            errors.Add($"{fieldLabel}.zip is {Zip.Trim().Length} characters; max is 10. Received \"{Zip.Trim()}\".");
        if (!string.IsNullOrWhiteSpace(Phone) && Phone.Trim().Length > 25)
            errors.Add($"{fieldLabel}.phone is {Phone.Trim().Length} characters; max is 25. Received \"{Phone.Trim()}\".");

        if (ContactCard != null && !ContactCard.IsEmpty())
        {
            if (!Enum.IsDefined(typeof(CardType), ContactCard.CardTypeId))
                errors.Add($"{fieldLabel}.contactCard.cardTypeId must be 0=Visa, 1=MasterCard, 2=Discover, or 3=AmericanExpress. Received {ContactCard.CardTypeId}.");
            if (string.IsNullOrWhiteSpace(ContactCard.CardName))
                errors.Add($"{fieldLabel}.contactCard.cardName is required.");
            if (string.IsNullOrWhiteSpace(ContactCard.CardNumber))
                errors.Add($"{fieldLabel}.contactCard.cardNumber is required.");
        }

        return errors;
    }

    public Contact ToNewOwnerContactModel(Guid organizationId, int officeId, string contactCode, Guid currentUser)
    {
        var ownerType = ResolveOwnerType(required: true) ?? OwnerType.Individual;
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
            State = UsStateCode.Normalize(TrimOrNull(State)),
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
            State = UsStateCode.Normalize(TrimOrNull(State)),
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
        contact.State = UsStateCode.Normalize(TrimOrNull(State)) ?? contact.State;
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
        contact.State = UsStateCode.Normalize(TrimOrNull(State)) ?? contact.State;
        contact.Zip = TrimOrNull(Zip) ?? contact.Zip;
        var ownerType = ResolveOwnerType(required: false);
        if (ownerType.HasValue)
            contact.OwnerType = ownerType.Value;
        if (!string.IsNullOrWhiteSpace(CompanyName))
            contact.CompanyName = CompanyName.Trim();
        contact.ModifiedBy = modifiedBy;
    }

    private OwnerType? ResolveOwnerType(bool required)
    {
        if (OwnerTypeId.HasValue && Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            return (OwnerType)OwnerTypeId.Value;

        if (!OwnerTypeId.HasValue && !required)
            return null;

        return string.IsNullOrWhiteSpace(CompanyName) ? OwnerType.Individual : OwnerType.Company;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
