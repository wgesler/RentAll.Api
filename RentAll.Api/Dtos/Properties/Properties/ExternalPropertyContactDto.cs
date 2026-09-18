using RentAll.Api.Dtos.Contacts.ContactCards;
using RentAll.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    [JsonConverter(typeof(ContactEntityTypeIdJsonConverter))]
    public int EntityTypeId { get; set; }
    public int? OwnerTypeId { get; set; }
    public int? VendorTypeId { get; set; }
    public string? CompanyName { get; set; }
    [JsonConverter(typeof(CardTypeIdJsonConverter))]
    public int? CardTypeId { get; set; }
    public string? CardName { get; set; }
    public string? CardNumber { get; set; }
    public UpsertContactCardDto? ContactCard { get; set; }

    public static bool IsAllowedContactEntityType(int entityTypeId) =>
        entityTypeId is (int)EntityType.Company or (int)EntityType.Owner or (int)EntityType.Tenant or (int)EntityType.Vendor;

    public static bool TryCoerceContactEntityType(JsonElement element, out int entityTypeId)
    {
        entityTypeId = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out entityTypeId) && IsAllowedContactEntityType(entityTypeId);

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0)
            return false;

        if (int.TryParse(raw, out entityTypeId))
            return IsAllowedContactEntityType(entityTypeId);

        if (Enum.TryParse<EntityType>(raw, ignoreCase: true, out var named) && IsAllowedContactEntityType((int)named))
        {
            entityTypeId = (int)named;
            return true;
        }

        var fromCode = EntityTypeExtensions.FromCode(raw);
        if (!IsAllowedContactEntityType((int)fromCode))
            return false;

        entityTypeId = (int)fromCode;
        return true;
    }

    public static bool TryCoerceCardType(JsonElement element, out int cardTypeId)
    {
        cardTypeId = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out cardTypeId) && Enum.IsDefined(typeof(CardType), cardTypeId);

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0)
            return false;

        if (int.TryParse(raw, out cardTypeId))
            return Enum.IsDefined(typeof(CardType), cardTypeId);

        var compact = raw.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (Enum.TryParse<CardType>(compact, ignoreCase: true, out var named) || Enum.TryParse<CardType>(raw, ignoreCase: true, out named))
        {
            cardTypeId = (int)named;
            return true;
        }

        cardTypeId = compact.ToLowerInvariant() switch
        {
            "visa" => (int)CardType.Visa,
            "mc" or "mastercard" => (int)CardType.MasterCard,
            "disc" or "discover" => (int)CardType.Discover,
            "amex" or "americanexpress" => (int)CardType.AmericanExpress,
            _ => -1
        };

        return cardTypeId >= 0;
    }

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

        if (!IsAllowedContactEntityType(EntityTypeId))
            errors.Add(EntityTypeId == 0
                ? $"{fieldLabel}.entityTypeId is required. Use 3=Company, 4=Owner, 5=Tenant, or 6=Vendor."
                : $"{fieldLabel}.entityTypeId must be 3=Company, 4=Owner, 5=Tenant, or 6=Vendor. Received {EntityTypeId}.");

        if (OwnerTypeId.HasValue && !Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            errors.Add($"{fieldLabel}.ownerTypeId must be 0=Individual or 1=Company. Received {OwnerTypeId.Value}.");
        if (VendorTypeId.HasValue && !Enum.IsDefined(typeof(VendorType), VendorTypeId.Value))
            errors.Add($"{fieldLabel}.vendorTypeId must be 0=Individual or 1=Company. Received {VendorTypeId.Value}.");

        var card = GetResolvedContactCard();
        var hasCardInput = CardTypeId.HasValue
            || !string.IsNullOrWhiteSpace(CardName)
            || !string.IsNullOrWhiteSpace(CardNumber)
            || (ContactCard != null && !ContactCard.IsEmpty());
        if (hasCardInput)
        {
            var cardTypeId = CardTypeId ?? ContactCard?.CardTypeId;
            if (!cardTypeId.HasValue || !Enum.IsDefined(typeof(CardType), cardTypeId.Value))
                errors.Add($"{fieldLabel}.cardTypeId must be 0=Visa, 1=MasterCard, 2=Discover, or 3=AmericanExpress. Received {(cardTypeId.HasValue ? cardTypeId.Value.ToString() : "missing")}.");
            if (string.IsNullOrWhiteSpace(card?.CardName))
                errors.Add($"{fieldLabel}.cardName is required.");
            if (string.IsNullOrWhiteSpace(card?.CardNumber))
                errors.Add($"{fieldLabel}.cardNumber is required.");
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
            EntityType = ResolveEntityType(EntityType.Owner),
            OwnerType = ownerType,
            VendorType = ResolveVendorType(VendorType.Individual),
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
            EntityType = ResolveEntityType(EntityType.Vendor),
            OwnerType = ResolveOwnerType(required: false) ?? OwnerType.Individual,
            VendorType = ResolveVendorType(VendorType.Company),
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
        if (VendorTypeId.HasValue && Enum.IsDefined(typeof(VendorType), VendorTypeId.Value))
            contact.VendorType = (VendorType)VendorTypeId.Value;
        var ownerType = ResolveOwnerType(required: false);
        if (ownerType.HasValue)
            contact.OwnerType = ownerType.Value;
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
        if (VendorTypeId.HasValue && Enum.IsDefined(typeof(VendorType), VendorTypeId.Value))
            contact.VendorType = (VendorType)VendorTypeId.Value;
        contact.ModifiedBy = modifiedBy;
    }

    public EntityType ResolveEntityType(EntityType fallback) =>
        IsAllowedContactEntityType(EntityTypeId) ? (EntityType)EntityTypeId : fallback;

    public UpsertContactCardDto? GetResolvedContactCard()
    {
        if (ContactCard != null && !ContactCard.IsEmpty())
            return ContactCard;

        var cardName = (CardName ?? string.Empty).Trim();
        var cardNumber = (CardNumber ?? string.Empty).Trim();
        if (!CardTypeId.HasValue && cardName.Length == 0 && cardNumber.Length == 0)
            return null;

        return new UpsertContactCardDto
        {
            CardTypeId = CardTypeId ?? 0,
            CardName = cardName,
            CardNumber = cardNumber
        };
    }

    private OwnerType? ResolveOwnerType(bool required)
    {
        if (OwnerTypeId.HasValue && Enum.IsDefined(typeof(OwnerType), OwnerTypeId.Value))
            return (OwnerType)OwnerTypeId.Value;

        if (!OwnerTypeId.HasValue && !required)
            return null;

        return string.IsNullOrWhiteSpace(CompanyName) ? OwnerType.Individual : OwnerType.Company;
    }

    private VendorType ResolveVendorType(VendorType fallback)
    {
        if (VendorTypeId.HasValue && Enum.IsDefined(typeof(VendorType), VendorTypeId.Value))
            return (VendorType)VendorTypeId.Value;

        return fallback;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public sealed class ContactEntityTypeIdJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (ExternalPropertyContactDto.TryCoerceContactEntityType(document.RootElement, out var entityTypeId))
            return entityTypeId;

        throw new JsonException("entityTypeId must be 3=Company, 4=Owner, 5=Tenant, or 6=Vendor.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}

public sealed class CardTypeIdJsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind == JsonValueKind.Null)
            return null;

        if (ExternalPropertyContactDto.TryCoerceCardType(document.RootElement, out var cardTypeId))
            return cardTypeId;

        throw new JsonException("cardTypeId must be 0=Visa, 1=MasterCard, 2=Discover, or 3=AmericanExpress.");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
