using RentAll.Api.Dtos.Leads;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentAll.Api.Dtos.External;

public static class ExternalIntakeErrors
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new FlexibleJsonBooleanConverter(), new FlexibleNullableJsonBooleanConverter() }
    };

    public static string Join(IEnumerable<string> errors) => string.Join("\n", errors.Where(error => !string.IsNullOrWhiteSpace(error)).Distinct());

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

    public static string FromJsonException(JsonException ex, JsonElement body)
    {
        var path = (ex.Path ?? string.Empty).Trim().TrimStart('$', '.');
        var field = string.IsNullOrWhiteSpace(path) ? "request" : path;
        if (TryGetProperty(body, field, out var element))
            return $"{field} has the wrong type. Received {Describe(element)}.";

        return $"{field} has the wrong type.";
    }

    public static T? Deserialize<T>(JsonElement body) => JsonSerializer.Deserialize<T>(body, JsonOptions);

    public static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
    {
        if (body.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in body.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public static void CollectRequiredGuid(JsonElement body, string field, List<string> errors, string requiredMessage)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(requiredMessage);
            return;
        }

        if (element.TryGetGuid(out var value) && value != Guid.Empty)
            return;

        errors.Add(element.ValueKind == JsonValueKind.String
            ? $"{field} must be a valid GUID. Received \"{element.GetString()?.Trim()}\"."
            : $"{field} must be a GUID string. Received {Describe(element)}.");
    }

    public static void CollectRequiredPositiveInt(JsonElement body, string field, List<string> errors, string requiredMessage)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(requiredMessage);
            return;
        }

        if (TryCoerceInt32(element, out var value) && value > 0)
            return;

        if (TryCoerceInt32(element, out value))
        {
            errors.Add($"{field} must be greater than 0. Received {value}.");
            return;
        }

        errors.Add($"{field} must be an integer. Received {Describe(element)}.");
    }

    public static void CollectRequiredString(JsonElement body, string field, List<string> errors, string requiredMessage)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(requiredMessage);
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{field} must be a string. Received {Describe(element)}.");
            return;
        }

        if (string.IsNullOrWhiteSpace(element.GetString()))
            errors.Add(requiredMessage);
    }

    public static void CollectRequiredEmail(JsonElement body, string field, List<string> errors, string requiredMessage, string invalidFormatMessage)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(requiredMessage);
            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            errors.Add($"{field} must be a string. Received {Describe(element)}.");
            return;
        }

        var email = element.GetString();
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(requiredMessage);
            return;
        }

        if (!LeadDtoValidation.IsValidEmail(email))
            errors.Add(invalidFormatMessage);
    }

    public static void CollectRequiredBool(JsonElement body, string field, List<string> errors, string requiredMessage)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
        {
            errors.Add(requiredMessage);
            return;
        }

        if (!TryCoerceBool(element, out _))
            errors.Add($"{field} must be true/false or 0/1. Received {Describe(element)}.");
    }

    public static void CollectOptionalString(JsonElement body, string field, List<string> errors)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (element.ValueKind != JsonValueKind.String)
            errors.Add($"{field} must be a string. Received {Describe(element)}.");
    }

    public static void CollectOptionalNonNegativeInt(JsonElement body, string field, List<string> errors)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceInt32(element, out var value))
        {
            errors.Add($"{field} must be an integer. Received {Describe(element)}.");
            return;
        }

        if (value < 0)
            errors.Add($"{field} cannot be negative. Received {value}.");
    }

    public static void CollectOptionalNonNegativeDecimal(JsonElement body, string field, List<string> errors)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceDecimal(element, out var value))
        {
            errors.Add($"{field} must be a number. Received {Describe(element)}.");
            return;
        }

        if (value < 0)
            errors.Add($"{field} cannot be negative. Received {value}.");
    }

    public static void CollectOptionalBool(JsonElement body, string field, List<string> errors)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (!TryCoerceBool(element, out _))
            errors.Add($"{field} must be true/false or 0/1. Received {Describe(element)}.");
    }

    public static void CollectOptionalDateOnly(JsonElement body, string field, List<string> errors)
    {
        if (!TryGetProperty(body, field, out var element) || element.ValueKind == JsonValueKind.Null)
            return;

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString()?.Trim() ?? string.Empty;
            if (raw.Length == 0 || DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return;

            errors.Add($"{field} must be a date (YYYY-MM-DD). Received \"{raw}\".");
            return;
        }

        errors.Add($"{field} must be a date string (YYYY-MM-DD). Received {Describe(element)}.");
    }

    public static void CollectOptionalStrings(JsonElement body, IEnumerable<string> fields, List<string> errors)
    {
        foreach (var field in fields)
            CollectOptionalString(body, field, errors);
    }

    public static void CollectOptionalBools(JsonElement body, IEnumerable<string> fields, List<string> errors)
    {
        foreach (var field in fields)
            CollectOptionalBool(body, field, errors);
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

    private sealed class FlexibleJsonBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TryReadFlexibleBoolean(ref reader, out var value))
                return value;

            throw new JsonException("Boolean value is invalid.");
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
    }

    private sealed class FlexibleNullableJsonBooleanConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (TryReadFlexibleBoolean(ref reader, out var value))
                return value;

            throw new JsonException("Boolean value is invalid.");
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteBooleanValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    private static bool TryReadFlexibleBoolean(ref Utf8JsonReader reader, out bool value)
    {
        value = false;
        if (reader.TokenType == JsonTokenType.True)
        {
            value = true;
            return true;
        }

        if (reader.TokenType == JsonTokenType.False)
            return true;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            if (number == 1)
            {
                value = true;
                return true;
            }

            if (number == 0)
                return true;
        }

        if (reader.TokenType != JsonTokenType.String)
            return false;

        var raw = reader.GetString()?.Trim();
        if (bool.TryParse(raw, out value))
            return true;

        if (string.Equals(raw, "1", StringComparison.Ordinal))
        {
            value = true;
            return true;
        }

        return string.Equals(raw, "0", StringComparison.Ordinal);
    }
}
