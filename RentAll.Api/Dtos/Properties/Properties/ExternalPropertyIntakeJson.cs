using System.Text.Json;
using System.Text.Json.Serialization;

namespace RentAll.Api.Dtos.Properties.Properties;

public static class ExternalPropertyIntakeJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new FlexibleJsonBooleanConverter(), new FlexibleNullableJsonBooleanConverter() }
    };

    private static readonly HashSet<string> RequestHeaderFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "organizationId",
        "officeId",
        "vendorId"
    };

    public static CreateExternalPropertyDto DeserializePropertyItem(JsonElement propertyElement)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in propertyElement.EnumerateObject())
            {
                if (RequestHeaderFields.Contains(property.Name))
                    continue;

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        stream.Position = 0;
        return JsonSerializer.Deserialize<CreateExternalPropertyDto>(stream, JsonOptions)
            ?? throw new JsonException("Property item deserialized to null");
    }

    public static JsonElement StripRequestHeaderFields(JsonElement propertyElement)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in propertyElement.EnumerateObject())
            {
                if (RequestHeaderFields.Contains(property.Name))
                    continue;

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        stream.Position = 0;
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    internal static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
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

    public static (bool Success, ExternalPropertyIntakeContext? Context, string? ErrorMessage) TryParseIntakeContext(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, "Property data is required");

        if (!TryParseRequiredOrganizationId(body, out var organizationId, out var organizationError))
            return (false, null, organizationError);

        if (!TryParseRequiredOfficeId(body, out var officeId, out var officeError))
            return (false, null, officeError);

        if (!TryParseRequiredVendorId(body, out var vendorId, out var vendorError))
            return (false, null, vendorError);

        return (true, new ExternalPropertyIntakeContext
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PartnerVendorId = vendorId
        }, null);
    }

    public static (bool Success, JsonElement PropertiesElement, string? ErrorMessage) TryParseRequiredPropertiesArray(JsonElement body)
    {
        if (!TryGetProperty(body, "properties", out var propertiesElement))
            return (false, default, "Properties must contain at least one item");

        if (propertiesElement.ValueKind == JsonValueKind.Array)
        {
            if (propertiesElement.GetArrayLength() == 0)
                return (false, default, "Properties must contain at least one item");

            return (true, propertiesElement, null);
        }

        if (propertiesElement.ValueKind == JsonValueKind.Object)
            return (false, default, "Properties must be a JSON array. Received a single object; wrap the property in an array.");

        return (false, default, $"Properties must be a JSON array. Received {DescribeJsonValueKind(propertiesElement.ValueKind)}.");
    }

    public static bool TryParseRequiredOrganizationId(JsonElement body, out Guid organizationId, out string? errorMessage)
    {
        organizationId = Guid.Empty;
        if (!TryGetProperty(body, "organizationId", out var element))
        {
            errorMessage = "OrganizationId is required";
            return false;
        }

        if (element.TryGetGuid(out organizationId) && organizationId != Guid.Empty)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = element.ValueKind == JsonValueKind.String
            ? $"OrganizationId must be a valid GUID. Received \"{element.GetString()?.Trim()}\"."
            : $"OrganizationId must be a GUID string. Received {DescribeJsonValueKind(element.ValueKind)}.";
        return false;
    }

    public static bool TryParseRequiredOfficeId(JsonElement body, out int officeId, out string? errorMessage)
    {
        officeId = 0;
        if (!TryGetProperty(body, "officeId", out var element))
        {
            errorMessage = "OfficeId is required";
            return false;
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt32(out officeId) && officeId > 0)
            {
                errorMessage = null;
                return true;
            }

            errorMessage = officeId <= 0
                ? $"OfficeId must be greater than 0. Received {officeId}."
                : "OfficeId must be a valid integer.";
            return false;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            if (TryCoerceOfficeId(element, out officeId))
            {
                errorMessage = null;
                return true;
            }

            var raw = element.GetString()?.Trim() ?? string.Empty;
            errorMessage = string.IsNullOrWhiteSpace(raw)
                ? "OfficeId is required"
                : $"OfficeId must be a valid integer greater than 0. Received \"{raw}\".";
            return false;
        }

        errorMessage = $"OfficeId must be a JSON number. Received {DescribeJsonValueKind(element.ValueKind)}.";
        return false;
    }

    public static bool TryParseRequiredVendorId(JsonElement body, out Guid vendorId, out string? errorMessage)
    {
        vendorId = Guid.Empty;
        if (!TryGetProperty(body, "vendorId", out var element))
        {
            errorMessage = "VendorId is required";
            return false;
        }

        if (element.TryGetGuid(out vendorId) && vendorId != Guid.Empty)
        {
            errorMessage = null;
            return true;
        }

        errorMessage = element.ValueKind == JsonValueKind.String
            ? $"VendorId must be a valid GUID. Received \"{element.GetString()?.Trim()}\"."
            : $"VendorId must be a GUID string. Received {DescribeJsonValueKind(element.ValueKind)}.";
        return false;
    }

    public static bool TryCoerceOfficeId(JsonElement element, out int officeId)
    {
        officeId = 0;
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out officeId))
            return officeId > 0;

        if (element.ValueKind == JsonValueKind.String
            && int.TryParse(element.GetString()?.Trim(), out officeId))
            return officeId > 0;

        return false;
    }

    public static string? TryGetPropertyCodeForLogging(JsonElement body)
    {
        if (!TryGetProperty(body, "properties", out var propertiesElement))
            return null;

        if (propertiesElement.ValueKind == JsonValueKind.Object)
            return TryGetPropertyCodeFromPropertyElement(propertiesElement);

        if (propertiesElement.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var propertyElement in propertiesElement.EnumerateArray())
        {
            var propertyCode = TryGetPropertyCodeFromPropertyElement(propertyElement);
            if (!string.IsNullOrWhiteSpace(propertyCode))
                return propertyCode;
        }

        return null;
    }

    private static string? TryGetPropertyCodeFromPropertyElement(JsonElement propertyElement)
    {
        if (propertyElement.ValueKind != JsonValueKind.Object)
            return null;

        if (!TryGetProperty(propertyElement, "propertyCode", out var propertyCodeElement)
            || propertyCodeElement.ValueKind != JsonValueKind.String)
            return null;

        var propertyCode = propertyCodeElement.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(propertyCode) ? null : propertyCode;
    }

    private static string DescribeJsonValueKind(JsonValueKind kind)
        => kind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => "array",
            JsonValueKind.Object => "object",
            _ => kind.ToString().ToLowerInvariant()
        };

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
