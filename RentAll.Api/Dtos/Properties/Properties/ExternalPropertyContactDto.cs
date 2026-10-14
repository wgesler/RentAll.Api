using RentAll.Api.Dtos.Contacts.ContactCards;
using RentAll.Domain;
using RentAll.Domain.Models.Common;
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
    [JsonConverter(typeof(FlexibleNullableDateOnlyConverter))]
    public DateOnly? DateOfBirth { get; set; }
    [JsonConverter(typeof(FlexibleNullableEnumConverter<FederalTaxType>))]
    public int? FederalTaxClassificationId { get; set; }
    [JsonConverter(typeof(FlexibleNullableEnumConverter<OwnerIdType>))]
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    [JsonConverter(typeof(FlexibleNullableEnumConverter<OwnerDocumentType>))]
    public int? OwnerDocumentId { get; set; }
    [JsonConverter(typeof(ExternalContactImageJsonConverter))]
    public ExternalContactImageDto? DocumentFront { get; set; }
    [JsonConverter(typeof(ExternalContactImageJsonConverter))]
    public ExternalContactImageDto? DocumentBack { get; set; }
    [JsonConverter(typeof(FlexibleNullableEnumConverter<PropertyManagerType>))]
    public int? PropertyManagedBy { get; set; }
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

        CollectOwnerProfileErrors(fieldLabel, errors);

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
            DateOfBirth = DateOfBirth,
            FederalTaxClassification = DefinedEnum<FederalTaxType>(FederalTaxClassificationId),
            OwnerIdType = DefinedEnum<OwnerIdType>(OwnerIdTypeId),
            IdentificationNumber = TrimOrNull(IdentificationNumber),
            OwnerDocument = DefinedEnum<OwnerDocumentType>(OwnerDocumentId),
            PropertyManagedBy = DefinedEnum<PropertyManagerType>(PropertyManagedBy),
            Rating = 0,
            IsInternational = false,
            Markup = 0,
            RevenueSplitOwner = 75,
            RevenueSplitOffice = 25,
            WorkingCapitalBalance = 0,
            LinenAndTowelFee = 0,
            IsActive = true,
            CreatedBy = currentUser
        };
    }

    public Contact ToNewCompanyContactModel(Guid organizationId, int officeId, string contactCode, Guid currentUser)
    {
        EntityTypeId = (int)EntityType.Company;
        var contact = ToNewOwnerContactModel(organizationId, officeId, contactCode, currentUser);
        contact.EntityType = EntityType.Company;
        return contact;
    }

    public Contact ToNewTenantContactModel(Guid organizationId, int officeId, string contactCode, Guid currentUser)
    {
        EntityTypeId = (int)EntityType.Tenant;
        var contact = ToNewOwnerContactModel(organizationId, officeId, contactCode, currentUser);
        contact.EntityType = EntityType.Tenant;
        contact.OwnerType = OwnerType.Individual;
        contact.VendorType = VendorType.Individual;
        return contact;
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
            DateOfBirth = DateOfBirth,
            FederalTaxClassification = DefinedEnum<FederalTaxType>(FederalTaxClassificationId),
            OwnerIdType = DefinedEnum<OwnerIdType>(OwnerIdTypeId),
            IdentificationNumber = TrimOrNull(IdentificationNumber),
            OwnerDocument = DefinedEnum<OwnerDocumentType>(OwnerDocumentId),
            PropertyManagedBy = DefinedEnum<PropertyManagerType>(PropertyManagedBy),
            Rating = 0,
            IsInternational = false,
            Markup = 0,
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
        ApplyOwnerProfile(contact);
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
        ApplyOwnerProfile(contact);
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

    private void CollectOwnerProfileErrors(string fieldLabel, List<string> errors)
    {
        if (FederalTaxClassificationId.HasValue && !Enum.IsDefined(typeof(FederalTaxType), FederalTaxClassificationId.Value))
            errors.Add($"{fieldLabel}.federalTaxClassificationId must be 0=IndividualProprietor, 1=CCorporation, 2=SCorporation, 3=Partnership, or 4=TrustEstate. Received {FederalTaxClassificationId.Value}.");
        if (OwnerIdTypeId.HasValue && !Enum.IsDefined(typeof(OwnerIdType), OwnerIdTypeId.Value))
            errors.Add($"{fieldLabel}.ownerIdTypeId must be 0=SocialSecurity or 1=EmployerIdentification. Received {OwnerIdTypeId.Value}.");
        if (OwnerDocumentId.HasValue && !Enum.IsDefined(typeof(OwnerDocumentType), OwnerDocumentId.Value))
            errors.Add($"{fieldLabel}.ownerDocumentId must be 0=DriversLicense, 1=Passport, or 2=IdCard. Received {OwnerDocumentId.Value}.");
        if (PropertyManagedBy.HasValue && !Enum.IsDefined(typeof(PropertyManagerType), PropertyManagedBy.Value))
            errors.Add($"{fieldLabel}.propertyManagedBy must be 0=Owner or 1=PropertyManagementCompany. Received {PropertyManagedBy.Value}.");
        if (!string.IsNullOrWhiteSpace(IdentificationNumber) && IdentificationNumber.Trim().Length > 9)
            errors.Add($"{fieldLabel}.identificationNumber is {IdentificationNumber.Trim().Length} characters; max is 9. Received \"{IdentificationNumber.Trim()}\".");
        DocumentFront?.CollectErrors(fieldLabel, "documentFront", errors);
        DocumentBack?.CollectErrors(fieldLabel, "documentBack", errors);
    }

    private void ApplyOwnerProfile(Contact contact)
    {
        if (DateOfBirth.HasValue)
            contact.DateOfBirth = DateOfBirth;
        if (FederalTaxClassificationId.HasValue && Enum.IsDefined(typeof(FederalTaxType), FederalTaxClassificationId.Value))
            contact.FederalTaxClassification = (FederalTaxType)FederalTaxClassificationId.Value;
        if (OwnerIdTypeId.HasValue && Enum.IsDefined(typeof(OwnerIdType), OwnerIdTypeId.Value))
            contact.OwnerIdType = (OwnerIdType)OwnerIdTypeId.Value;
        var identificationNumber = TrimOrNull(IdentificationNumber);
        if (identificationNumber != null)
            contact.IdentificationNumber = identificationNumber;
        if (OwnerDocumentId.HasValue && Enum.IsDefined(typeof(OwnerDocumentType), OwnerDocumentId.Value))
            contact.OwnerDocument = (OwnerDocumentType)OwnerDocumentId.Value;
        if (PropertyManagedBy.HasValue && Enum.IsDefined(typeof(PropertyManagerType), PropertyManagedBy.Value))
            contact.PropertyManagedBy = (PropertyManagerType)PropertyManagedBy.Value;
    }

    private static TEnum? DefinedEnum<TEnum>(int? value) where TEnum : struct, Enum =>
        value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value) ? (TEnum)Enum.ToObject(typeof(TEnum), value.Value) : null;

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

public class ExternalContactImageDto
{
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? File { get; set; }
    public string? DataUrl { get; set; }

    public void CollectErrors(string fieldLabel, string fieldName, List<string> errors)
    {
        if (!TryGetImageContent(out _, out var error))
            errors.Add($"{fieldLabel}.{fieldName} {error}");
    }

    public FileDetails ToFileDetails(string defaultFileName)
    {
        TryGetImageContent(out var content, out _);
        var contentType = ResolveContentType(content);
        return new FileDetails
        {
            FileName = string.IsNullOrWhiteSpace(FileName) ? defaultFileName : FileName.Trim(),
            ContentType = contentType,
            File = content
        };
    }

    private bool TryGetImageContent(out string content, out string error)
    {
        content = FirstNonEmpty(File, DataUrl);
        error = string.Empty;
        if (content.Length == 0)
        {
            error = "must be an image. Send file or dataUrl as base64, or a data:image URL.";
            return false;
        }

        var payload = content;
        var declaredType = (ContentType ?? string.Empty).Trim();
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = payload.IndexOf(',');
            if (comma < 0 || comma == payload.Length - 1)
            {
                error = "must be a data:image URL.";
                return false;
            }

            var header = payload[..comma];
            if (!header.Contains("image/", StringComparison.OrdinalIgnoreCase))
            {
                error = "must be an image. Received a non-image data URL.";
                return false;
            }

            payload = payload[(comma + 1)..];
            if (declaredType.Length == 0)
                declaredType = header.Replace("data:", "", StringComparison.OrdinalIgnoreCase).Split(';')[0];
        }

        if (declaredType.Length > 0 && !declaredType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            error = $"must be an image content type. Received \"{declaredType}\".";
            return false;
        }

        try
        {
            _ = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            error = "must be a base64 image.";
            return false;
        }

        return true;
    }

    private string ResolveContentType(string content)
    {
        if (!string.IsNullOrWhiteSpace(ContentType))
            return ContentType.Trim();

        if (content.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = content.IndexOf(',');
            if (comma > 5)
                return content[5..comma].Split(';')[0];
        }

        return "image/jpeg";
    }

    private static string FirstNonEmpty(string? left, string? right)
    {
        var first = (left ?? string.Empty).Trim();
        return first.Length > 0 ? first : (right ?? string.Empty).Trim();
    }
}

public sealed class ExternalContactImageJsonConverter : JsonConverter<ExternalContactImageDto?>
{
    public override ExternalContactImageDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var element = document.RootElement;
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return new ExternalContactImageDto { DataUrl = raw };
        }

        if (element.ValueKind != JsonValueKind.Object)
            throw new JsonException("Document image must be an object or a base64 image string.");

        return new ExternalContactImageDto
        {
            FileName = ReadString(element, "fileName"),
            ContentType = ReadString(element, "contentType"),
            File = ReadString(element, "file"),
            DataUrl = ReadString(element, "dataUrl")
        };
    }

    public override void Write(Utf8JsonWriter writer, ExternalContactImageDto? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("fileName", value.FileName);
        writer.WriteString("contentType", value.ContentType);
        writer.WriteString("file", value.File);
        writer.WriteString("dataUrl", value.DataUrl);
        writer.WriteEndObject();
    }

    private static string? ReadString(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) || property.Value.ValueKind != JsonValueKind.String)
                continue;

            var raw = property.Value.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }

        return null;
    }
}

public sealed class FlexibleNullableDateOnlyConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        if (!ExternalContactValueCoercion.TryReadDateOnly(document.RootElement, out var date, out var error))
            throw new JsonException(error);

        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        else
            writer.WriteNullValue();
    }
}

public sealed class FlexibleNullableEnumConverter<TEnum> : JsonConverter<int?> where TEnum : struct, Enum
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        if (!ExternalContactValueCoercion.TryReadEnum<TEnum>(document.RootElement, out var value, out var error))
            throw new JsonException(error);

        return value;
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

public static class ExternalContactValueCoercion
{
    public static bool TryReadDateOnly(JsonElement element, out DateOnly? date, out string error)
    {
        date = null;
        error = "must be a date such as yyyy-MM-dd.";
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0)
            return true;

        if (DateOnly.TryParse(raw, out var parsed))
        {
            date = parsed;
            error = string.Empty;
            return true;
        }

        return false;
    }

    public static bool TryReadEnum<TEnum>(JsonElement element, out int? value, out string error) where TEnum : struct, Enum
    {
        value = null;
        error = $"must be {DescribeEnum<TEnum>()}.";
        if (element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return true;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) && Enum.IsDefined(typeof(TEnum), number))
        {
            value = number;
            error = string.Empty;
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0)
            return true;

        if (int.TryParse(raw, out number) && Enum.IsDefined(typeof(TEnum), number))
        {
            value = number;
            error = string.Empty;
            return true;
        }

        var compact = raw.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);
        if (Enum.TryParse<TEnum>(compact, ignoreCase: true, out var named) && Enum.IsDefined(typeof(TEnum), named))
        {
            value = Convert.ToInt32(named);
            error = string.Empty;
            return true;
        }

        return false;
    }

    public static string DescribeEnum<TEnum>() where TEnum : struct, Enum
    {
        var names = Enum.GetValues<TEnum>().Select(value => $"{Convert.ToInt32(value)}={value}");
        return string.Join(", ", names);
    }
}
