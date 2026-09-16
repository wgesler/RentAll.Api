using System.Text.Json;

namespace RentAll.Api.Dtos.Properties.Properties;

public static class ExternalPropertyIntakeJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
}
