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
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");

        if (OfficeId <= 0)
            errors.Add("OfficeId is required");

        if (VendorId == Guid.Empty)
            errors.Add("VendorId is required");

        if (Properties == null || Properties.Count == 0)
            errors.Add("Properties must contain at least one item");
        else if (Properties.Count > MaxPropertiesPerRequest)
            errors.Add($"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

        if (Properties != null)
        {
            for (var index = 0; index < Properties.Count; index++)
            {
                var (itemIsValid, itemError) = Properties[index].IsValid();
                if (!itemIsValid)
                    errors.Add($"Properties[{index}]: {itemError}");
            }
        }

        return errors.Count == 0 ? (true, null) : (false, ExternalPropertyIntakeErrors.Join(errors));
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

        var errors = new List<string>();
        ExternalPropertyIntakeJson.CollectIntakeContextErrors(body, errors, out var context);

        var properties = new List<CreateExternalPropertyDto>();
        var (propertiesParsed, propertiesElement, propertiesError) = ExternalPropertyIntakeJson.TryParseRequiredPropertiesArray(body);
        if (!propertiesParsed)
            errors.Add(propertiesError ?? "Properties must contain at least one item");
        else
        {
            if (propertiesElement.GetArrayLength() > MaxPropertiesPerRequest)
                errors.Add($"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

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
        }

        if (errors.Count > 0 || context == null)
            return (false, null, null, errors.Count > 0 ? ExternalPropertyIntakeErrors.Join(errors) : "Invalid request data");

        return (true, context, properties, null);
    }
}
