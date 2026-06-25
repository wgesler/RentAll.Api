namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class PropertyAgreementRentRollResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public List<PropertyAgreementLineResponseDto> AgreementLines { get; set; } = new();

    public PropertyAgreementRentRollResponseDto(PropertyAgreementRentRoll model)
    {
        PropertyId = model.PropertyId;
        PropertyCode = model.PropertyCode;
        OfficeId = model.OfficeId;
        AgreementLines = (model.AgreementLines ?? Enumerable.Empty<AgreementLine>()).Select(line => new PropertyAgreementLineResponseDto(line)).ToList();
    }
}
