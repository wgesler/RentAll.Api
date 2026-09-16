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

        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "organizationId", out var organizationIdElement)
            || !organizationIdElement.TryGetGuid(out var organizationId)
            || organizationId == Guid.Empty)
            return (false, null, null, "OrganizationId is required");

        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "officeId", out var officeIdElement)
            || officeIdElement.ValueKind != JsonValueKind.Number
            || !officeIdElement.TryGetInt32(out var officeId)
            || officeId <= 0)
            return (false, null, null, "OfficeId is required");

        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "vendorId", out var vendorIdElement)
            || !vendorIdElement.TryGetGuid(out var vendorId)
            || vendorId == Guid.Empty)
            return (false, null, null, "VendorId is required");

        if (!ExternalPropertyIntakeJson.TryGetProperty(body, "properties", out var propertiesElement)
            || propertiesElement.ValueKind != JsonValueKind.Array)
            return (false, null, null, "Properties must contain at least one item");

        var properties = new List<CreateExternalPropertyDto>();
        var index = 0;
        foreach (var propertyElement in propertiesElement.EnumerateArray())
        {
            if (propertyElement.ValueKind != JsonValueKind.Object)
                return (false, null, null, $"Properties[{index}] must be an object");

            CreateExternalPropertyDto propertyDto;
            try
            {
                propertyDto = ExternalPropertyIntakeJson.DeserializePropertyItem(propertyElement);
            }
            catch (JsonException)
            {
                return (false, null, null, $"Properties[{index}]: Invalid property JSON");
            }

            var (itemIsValid, itemError) = propertyDto.IsValid();
            if (!itemIsValid)
                return (false, null, null, $"Properties[{index}]: {itemError}");

            properties.Add(propertyDto);
            index++;
        }

        if (properties.Count == 0)
            return (false, null, null, "Properties must contain at least one item");

        if (properties.Count > MaxPropertiesPerRequest)
            return (false, null, null, $"Properties cannot exceed {MaxPropertiesPerRequest} items per request");

        return (true, new ExternalPropertyIntakeContext
        {
            OrganizationId = organizationId,
            OfficeId = officeId,
            PartnerVendorId = vendorId
        }, properties, null);
    }
}
