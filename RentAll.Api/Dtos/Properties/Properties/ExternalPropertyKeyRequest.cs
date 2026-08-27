namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyKeyRequest
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) ValidateRequiredKeys()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (VendorId == Guid.Empty)
            return (false, "VendorId is required");

        if (string.IsNullOrWhiteSpace(PropertyCode))
            return (false, "PropertyCode is required");

        return (true, null);
    }

    public ExternalPropertyKeyDto ToKeyDto()
    {
        return new ExternalPropertyKeyDto
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            VendorId = VendorId,
            PropertyCode = PropertyCode.Trim()
        };
    }
}
