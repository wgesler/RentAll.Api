namespace RentAll.Domain.Models;

public class PropertyAgreementRentRoll
{
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public List<AgreementLine> AgreementLines { get; set; } = new();
}
