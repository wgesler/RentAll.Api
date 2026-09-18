using RentAll.Domain;
using System.Globalization;
using System.Text.Json;

namespace RentAll.Api.Dtos.Properties.Properties;

public static class ExternalPropertyIntakeErrors
{
    private static readonly string[] IntegerFields = ["bedrooms", "accommodates", "squareFeet", "minStay", "maxStay"];
    private static readonly string[] DecimalFields = ["bathrooms", "monthlyRate", "dailyRate", "departureFee", "maidServiceFee", "petFee"];
    private static readonly string[] BooleanFields = ["isActive", "unfurnished", "heating", "ac", "elevator", "security", "gated", "petsAllowed", "dogsOkay", "catsOkay", "smoking", "parking", "kitchen", "oven", "refrigerator", "microwave", "dishwasher", "bathtub", "washerDryerInUnit", "washerDryerInBldg", "tv", "cable", "dvd", "streaming", "fastInternet", "deck", "patio", "yard", "garden", "commonPool", "privatePool", "jacuzzi", "sauna", "gym"];
    private static readonly string[] RequiredStringFields = ["propertyCode", "address1", "city", "state", "zip", "description"];

    public static string Join(IEnumerable<string> errors) => string.Join("\n", errors.Where(error => !string.IsNullOrWhiteSpace(error)).Distinct());

    public static string Prefix(string prefix, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return $"{prefix}: Invalid request data";

        return Join(message.Split('\n').Select(line => line.StartsWith(prefix, StringComparison.Ordinal) ? line : $"{prefix}: {line}"));
    }

    public static string Describe(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => $"string \"{element.GetString()}\"",
            JsonValueKind.Number => $"number {element.GetRawText()}",
            JsonValueKind.True => "boolean true",
            JsonValueKind.False => "boolean false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => element.ValueKind.ToString().ToLowerInvariant()
        };
    }

    public static string DescribeField(JsonElement body, string name)
    {
        return ExternalPropertyIntakeJson.TryGetProperty(body, name, out var element) ? Describe(element) : "missing";
    }

    public static string FromJsonException(JsonException ex, JsonElement propertyElement, string prefix)
    {
        var path = (ex.Path ?? string.Empty).Trim().TrimStart('$', '.');
        var field = string.IsNullOrWhiteSpace(path) ? "property" : path;
        if (TryGetNestedProperty(propertyElement, path, out var element))
            return $"{prefix}.{field} has the wrong type. Received {Describe(element)}. {SimplifyJsonException(ex.Message)}";

        return $"{prefix}.{field} has the wrong type. {SimplifyJsonException(ex.Message)}";
    }

    public static List<string> CollectFromJson(JsonElement propertyElement, string prefix, bool requireCreateFields = true)
    {
        var errors = new List<string>();
        if (propertyElement.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{prefix} must be an object. Received {Describe(propertyElement)}.");
            return errors;
        }

        if (requireCreateFields)
        {
            foreach (var field in RequiredStringFields)
                CollectRequiredString(propertyElement, prefix, field, errors);
        }

        foreach (var field in IntegerFields)
            CollectInteger(propertyElement, prefix, field, errors);

        foreach (var field in DecimalFields)
            CollectDecimal(propertyElement, prefix, field, errors);

        foreach (var field in BooleanFields)
            CollectBoolean(propertyElement, prefix, field, errors);

        CollectEnum<PropertyLeaseType>(propertyElement, prefix, "propertyLeaseTypeId", errors, "0=PropertyManagement, 1=Direct, 2=ThirdParty");
        CollectEnum<PropertyStyle>(propertyElement, prefix, "propertyStyleId", errors, "0=Standard, 1=Corporate, 2=Vacation");
        CollectPropertyType(propertyElement, prefix, errors);
        CollectEnum<CheckInTime>(propertyElement, prefix, "checkInTimeId", errors, "1=12PM, 2=1PM, 3=2PM, 4=3PM, 5=4PM, 6=5PM");
        CollectEnum<CheckOutTime>(propertyElement, prefix, "checkOutTimeId", errors, "1=8AM, 2=9AM, 3=10AM, 4=11AM, 5=12PM, 6=1PM");
        CollectBedroomId(propertyElement, prefix, "bedroomId1", errors);
        CollectBedroomId(propertyElement, prefix, "bedroomId2", errors);
        CollectBedroomId(propertyElement, prefix, "bedroomId3", errors);
        CollectBedroomId(propertyElement, prefix, "bedroomId4", errors);
        CollectState(propertyElement, prefix, "state", errors);
        CollectMaxLength(propertyElement, prefix, "zip", 10, errors);
        CollectMaxLength(propertyElement, prefix, "address1", 100, errors);
        CollectMaxLength(propertyElement, prefix, "city", 100, errors);
        CollectContact(propertyElement, prefix, "owner1", errors);
        CollectContact(propertyElement, prefix, "owner2", errors);
        CollectContact(propertyElement, prefix, "owner3", errors);
        CollectContact(propertyElement, prefix, "vendor", errors);
        CollectPhotos(propertyElement, prefix, errors);
        if (requireCreateFields)
            CollectLeaseTypeContacts(propertyElement, prefix, errors);
        return errors;
    }

    public static string TranslateSaveError(string message)
    {
        if (message.Contains("truncated", StringComparison.OrdinalIgnoreCase))
            return "A text value is too long for the database. State max 2 characters (TX, not Texas if unmapped), Zip max 10, Phone max 25, Address1/City max 100. Original: " + Truncate(message);

        if (message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            return "A unique value already exists (often PropertyCode or Contact email). Original: " + Truncate(message);

        if (message.Contains("Cannot insert the value NULL", StringComparison.OrdinalIgnoreCase))
            return "A required database column was empty. Original: " + Truncate(message);

        return Truncate(message);
    }

    private static void CollectRequiredString(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add($"{prefix}.{field} is required.");
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{prefix}.{field} must be a string. Received {Describe(element)}.");
            return;
        }

        if (string.IsNullOrWhiteSpace(element.GetString()))
            errors.Add($"{prefix}.{field} is required.");
    }

    private static void CollectInteger(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceInt32(element, out _))
            errors.Add($"{prefix}.{field} must be an integer. Received {Describe(element)}.");
    }

    private static void CollectDecimal(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceDecimal(element, out _))
            errors.Add($"{prefix}.{field} must be a number. Received {Describe(element)}.");
    }

    private static void CollectBoolean(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceBool(element, out _))
            errors.Add($"{prefix}.{field} must be true/false or 0/1. Received {Describe(element)}.");
    }

    private static void CollectEnum<TEnum>(JsonElement body, string prefix, string field, List<string> errors, string allowed) where TEnum : struct, Enum
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceInt32(element, out var value))
        {
            errors.Add($"{prefix}.{field} must be an integer ({allowed}). Received {Describe(element)}.");
            return;
        }

        if (!Enum.IsDefined(typeof(TEnum), value))
            errors.Add($"{prefix}.{field} must be {allowed}. Received {value}.");
    }

    private static void CollectPropertyType(JsonElement body, string prefix, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "propertyTypeId", out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceInt32(element, out var value))
        {
            errors.Add($"{prefix}.propertyTypeId must be an integer (1=Apartment ... 8=House ... 17=Hotel). Received {Describe(element)}.");
            return;
        }

        if (!Enum.IsDefined(typeof(PropertyType), value) || value == (int)PropertyType.Unspecified)
            errors.Add($"{prefix}.propertyTypeId must be 1-17 (1=Apartment, 8=House, 5=Condo, ...). 0=Unspecified is not allowed. Received {value}.");
    }

    private static void CollectBedroomId(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceInt32(element, out var value))
        {
            errors.Add($"{prefix}.{field} must be an integer 0-7 (0=Unknown, 1=King, 2=Queen, 3=Double, 4=Twin, 5=TwoTwins, 6=DayBed, 7=SofaBed). Received {Describe(element)}.");
            return;
        }

        if (!Enum.IsDefined(typeof(BedSizeType), value))
            errors.Add($"{prefix}.{field} must be 0-7 (0=Unknown, 1=King, 2=Queen, 3=Double, 4=Twin, 5=TwoTwins, 6=DayBed, 7=SofaBed). Received {value}.");
    }

    private static void CollectState(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind != JsonValueKind.String)
            return;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length == 0)
            return;

        if (UsStateCode.IsRecognized(raw))
            return;

        var mapped = UsStateCode.Normalize(raw);
        errors.Add($"{prefix}.{field} must be a 2-letter US code or full state name. Received \"{raw}\"{(mapped != raw ? $" (mapped to \"{mapped}\")" : string.Empty)}. Example: \"TX\" or \"Texas\".");
    }

    private static void CollectMaxLength(JsonElement body, string prefix, string field, int maxLength, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind != JsonValueKind.String)
            return;

        var raw = element.GetString()?.Trim() ?? string.Empty;
        if (raw.Length > maxLength)
            errors.Add($"{prefix}.{field} is {raw.Length} characters; max is {maxLength}. Received \"{raw}\".");
    }

    private static void CollectContact(JsonElement body, string prefix, string field, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        var contactPrefix = $"{prefix}.{field}";
        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{contactPrefix} must be an object. Received {Describe(element)}.");
            return;
        }

        CollectRequiredString(element, contactPrefix, "firstName", errors);
        CollectRequiredString(element, contactPrefix, "lastName", errors);
        CollectRequiredString(element, contactPrefix, "email", errors);
        CollectState(element, contactPrefix, "state", errors);
        CollectMaxLength(element, contactPrefix, "zip", 10, errors);
        CollectMaxLength(element, contactPrefix, "phone", 25, errors);
        CollectMaxLength(element, contactPrefix, "address1", 100, errors);
        CollectMaxLength(element, contactPrefix, "city", 100, errors);
        if (ExternalPropertyIntakeJson.TryGetProperty(element, "ownerTypeId", out var ownerTypeElement) && ownerTypeElement.ValueKind != JsonValueKind.Null && !TryCoerceInt32(ownerTypeElement, out _))
            errors.Add($"{contactPrefix}.ownerTypeId must be an integer 0=Individual or 1=Company. Received {Describe(ownerTypeElement)}. Invalid values are ignored.");
        CollectContactCard(element, contactPrefix, errors);
    }

    private static void CollectContactCard(JsonElement contact, string contactPrefix, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(contact, "contactCard", out var card) || card.ValueKind == JsonValueKind.Null)
            return;

        if (card.ValueKind != JsonValueKind.Object)
        {
            errors.Add($"{contactPrefix}.contactCard must be an object. Received {Describe(card)}.");
            return;
        }

        var hasType = ExternalPropertyIntakeJson.TryGetProperty(card, "cardTypeId", out var typeElement) && typeElement.ValueKind != JsonValueKind.Null;
        var cardName = ExternalPropertyIntakeJson.TryGetProperty(card, "cardName", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        var cardNumber = ExternalPropertyIntakeJson.TryGetProperty(card, "cardNumber", out var numberElement) && numberElement.ValueKind == JsonValueKind.String
            ? numberElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;

        if (!hasType && cardName.Length == 0 && cardNumber.Length == 0)
            return;

        if (!hasType || !TryCoerceInt32(typeElement, out var cardTypeId) || !Enum.IsDefined(typeof(CardType), cardTypeId))
            errors.Add($"{contactPrefix}.contactCard.cardTypeId must be 0=Visa, 1=MasterCard, 2=Discover, or 3=AmericanExpress. Received {(hasType ? Describe(typeElement) : "missing")}.");
        if (cardName.Length == 0)
            errors.Add($"{contactPrefix}.contactCard.cardName is required.");
        if (cardNumber.Length == 0)
            errors.Add($"{contactPrefix}.contactCard.cardNumber is required.");
    }

    private static void CollectPhotos(JsonElement body, string prefix, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "photos", out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (element.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{prefix}.photos must be a JSON array of objects. Received {Describe(element)}.");
            return;
        }

        var index = 0;
        foreach (var photo in element.EnumerateArray())
        {
            if (photo.ValueKind != JsonValueKind.Object)
                errors.Add($"{prefix}.photos[{index}] must be an object. Received {Describe(photo)}.");
            index++;
        }
    }

    private static void CollectLeaseTypeContacts(JsonElement body, string prefix, List<string> errors)
    {
        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "propertyLeaseTypeId", out var leaseElement) || !TryCoerceInt32(leaseElement, out var leaseTypeId) || !Enum.IsDefined(typeof(PropertyLeaseType), leaseTypeId))
            return;

        var hasOwner1 = ExternalPropertyIntakeJson.TryGetProperty(body, "owner1", out var owner1) && owner1.ValueKind == JsonValueKind.Object;
        var hasVendor = ExternalPropertyIntakeJson.TryGetProperty(body, "vendor", out var vendor) && vendor.ValueKind == JsonValueKind.Object;
        if (leaseTypeId == (int)PropertyLeaseType.PropertyManagement && !hasOwner1)
            errors.Add($"{prefix}.owner1 is required when propertyLeaseTypeId is 0 (PropertyManagement).");
        if (leaseTypeId == (int)PropertyLeaseType.PropertyManagement && hasVendor)
            errors.Add($"{prefix}.vendor is not allowed when propertyLeaseTypeId is 0 (PropertyManagement).");
        if ((leaseTypeId == (int)PropertyLeaseType.Direct || leaseTypeId == (int)PropertyLeaseType.ThirdParty) && !hasVendor)
            errors.Add($"{prefix}.vendor is required when propertyLeaseTypeId is {leaseTypeId} ({(PropertyLeaseType)leaseTypeId}).");
    }

    private static bool TryGetNestedProperty(JsonElement body, string path, out JsonElement value)
    {
        value = body;
        if (string.IsNullOrWhiteSpace(path) || body.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ExternalPropertyIntakeJson.TryGetProperty(value, segment, out value))
                return false;
        }

        return true;
    }

    private static string SimplifyJsonException(string message)
    {
        if (message.Contains("Int32", StringComparison.OrdinalIgnoreCase) || message.Contains("Int64", StringComparison.OrdinalIgnoreCase))
            return "Expected an integer (2), not a quoted string (\"2\") unless it is numeric.";
        if (message.Contains("Decimal", StringComparison.OrdinalIgnoreCase) || message.Contains("Double", StringComparison.OrdinalIgnoreCase))
            return "Expected a number (1.5).";
        if (message.Contains("Boolean", StringComparison.OrdinalIgnoreCase))
            return "Expected true/false or 0/1.";
        if (message.Contains("Guid", StringComparison.OrdinalIgnoreCase))
            return "Expected a GUID string.";
        return Truncate(message);
    }

    private static bool TryCoerceInt32(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        return element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString()?.Trim(), out value);
    }

    private static bool TryCoerceDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out value);

        return element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString()?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryCoerceBool(JsonElement element, out bool value)
    {
        value = false;
        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
            return true;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number) && number is 0 or 1)
        {
            value = number == 1;
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString()?.Trim();
        if (bool.TryParse(raw, out value))
            return true;

        if (string.Equals(raw, "1", StringComparison.Ordinal))
        {
            value = true;
            return true;
        }

        return string.Equals(raw, "0", StringComparison.Ordinal);
    }

    private static string Truncate(string message) => message.Length > 300 ? message[..300] : message;
}
