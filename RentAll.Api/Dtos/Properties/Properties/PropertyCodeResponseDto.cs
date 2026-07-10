namespace RentAll.Api.Dtos.Properties.Properties;

public class PropertyCodeResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int PropertyLeaseTypeId { get; set; }
    public string ShortAddress { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;

    public PropertyCodeResponseDto(PropertyCodes propertyCode)
    {
        PropertyId = propertyCode.PropertyId;
        PropertyCode = propertyCode.PropertyCode;
        PropertyLeaseTypeId = propertyCode.PropertyLeaseTypeId;
        ShortAddress = propertyCode.ShortAddress;
        OfficeId = propertyCode.OfficeId;
        OfficeName = propertyCode.OfficeName;
    }
}
