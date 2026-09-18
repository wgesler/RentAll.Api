namespace RentAll.Api.Dtos.Properties.Properties;

public class ExternalPropertyKeyRequest
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public Guid VendorId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;

    public (bool IsValid, string? ErrorMessage) ValidateRequiredKeys()
    {
        var errors = new List<string>();
        if (OrganizationId == Guid.Empty)
            errors.Add("OrganizationId is required");
        if (OfficeId <= 0)
            errors.Add("OfficeId is required");
        if (VendorId == Guid.Empty)
            errors.Add("VendorId is required");
        if (string.IsNullOrWhiteSpace(PropertyCode))
            errors.Add("PropertyCode is required");
        return errors.Count == 0 ? (true, null) : (false, string.Join("\n", errors));
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
