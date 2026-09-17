using System.Text.Json;

namespace RentAll.Api.Dtos.Properties.Properties;

public class CreateExternalPropertyRequestDto
{
    public const int MaxPropertiesPerRequest = 50;

    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public List<CreateExternalPropertyDto> Properties { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (VendorId == Guid.Empty)
            return (false, "VendorId is required");

        if (Properties == null || Properties.Count == 0)
            return (false, "Properties must contain at least one item");

        if (Properties.Count > MaxPropertiesPerRequest)
            return (false, $"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

        for (var index = 0; index < Properties.Count; index++)
        {
            var (itemIsValid, itemError) = Properties[index].IsValid();
            if (!itemIsValid)
                return (false, $"Properties[{index}]: {itemError}");
        }

        return (true, null);
    }

    public ExternalPropertyIntakeContext ToIntakeContext()
    {
        return new ExternalPropertyIntakeContext
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            PartnerVendorId = VendorId
        };
    }

    public static (bool Success, ExternalPropertyIntakeContext? Context, List<CreateExternalPropertyDto>? Properties, string? ErrorMessage) TryParseFromBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object)
            return (false, null, null, "Property data is required");

        var (contextParsed, context, contextError) = ExternalPropertyIntakeJson.TryParseIntakeContext(body);
        if (!contextParsed || context == null)
            return (false, null, null, contextError ?? "Invalid request data");

        var (propertiesParsed, propertiesElement, propertiesError) = ExternalPropertyIntakeJson.TryParseRequiredPropertiesArray(body);
        if (!propertiesParsed)
            return (false, null, null, propertiesError ?? "Properties must contain at least one item");

        var properties = new List<CreateExternalPropertyDto>();
        var errors = new List<string>();
        var index = 0;
        foreach (var propertyElement in propertiesElement.EnumerateArray())
        {
            var prefix = $"Properties[{index}]";
            var itemErrors = ExternalPropertyIntakeErrors.CollectFromJson(propertyElement, prefix);
            if (itemErrors.Count == 0)
            {
                try
                {
                    var propertyDto = ExternalPropertyIntakeJson.DeserializePropertyItem(propertyElement);
                    var (itemIsValid, itemError) = propertyDto.IsValid();
                    if (!itemIsValid)
                        itemErrors.Add($"{prefix}: {itemError}");
                    else
                        properties.Add(propertyDto);
                }
                catch (JsonException ex)
                {
                    itemErrors.Add(ExternalPropertyIntakeErrors.FromJsonException(ex, propertyElement, prefix));
                }
            }

            errors.AddRange(itemErrors);
            index++;
        }

        if (errors.Count > 0)
            return (false, null, null, ExternalPropertyIntakeErrors.Join(errors));

        if (properties.Count == 0)
            return (false, null, null, "Properties must contain at least one item");

        if (properties.Count > MaxPropertiesPerRequest)
            return (false, null, null, $"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

        return (true, context, properties, null);
    }
}
