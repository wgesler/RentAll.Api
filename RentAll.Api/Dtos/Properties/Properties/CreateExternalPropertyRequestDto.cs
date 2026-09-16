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
}
